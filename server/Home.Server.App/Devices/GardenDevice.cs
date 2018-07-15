using System.Linq;
using Lucky.Home.Sinks;
using System;
using System.Threading;
using Lucky.Services;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Threading.Tasks;
using Lucky.Home.Model;
using System.Collections.Generic;
using Lucky.Db;
using Lucky.Home.Notification;
using Lucky.Home.Services;

namespace Lucky.Home.Devices
{
    class GardenCsvRecord
    {
        [Csv("yyyy-MM-dd")]
        public DateTime Date;

        /// <summary>
        /// Time of day of the first sample with power > 0
        /// </summary>
        [Csv("hh\\:mm\\:ss")]
        public TimeSpan Time;

        [Csv]
        public string Cycle;

        [Csv]
        public string Zones;

        /// <summary>
        /// 0: stoped (final). 1 = just started. 2 = flowing
        /// </summary>
        [Csv]
        public int State;

        /// <summary>
        /// In liter/seconds
        /// </summary>
        [Csv("0.0")]
        public double FlowLMin;

        /// <summary>
        /// Liter used for cycle
        /// </summary>
        [Csv("0.0")]
        public double QtyL;

        /// <summary>
        /// Total MC
        /// </summary>
        [Csv("0.000")]
        public double TotalQtyMc;
    }

    [Device("Garden")]
    [Requires(typeof(GardenSink))]
    [Requires(typeof(FlowSink))]
    public class GardenDevice : DeviceBase
    {
        private static int POLL_PERIOD = 3000;
        private FileInfo _cfgFile;
        private Timer _debounceTimer;
        private object _timeProgramLock = new object();
        private readonly TimeProgram<GardenCycle> _timeProgram;
        private readonly Queue<ImmediateProgram> _cycleQueue = new Queue<ImmediateProgram>();
        private readonly FileInfo _csvFile;
        private string[] _zoneNames = new string[0];
        private readonly double _counterFq;

        [DataContract]
        public class GardenCycle : TimeProgram<GardenCycle>.Cycle
        {
            [DataMember(Name = "zones")]
            public int[] Zones;
        }

        private class ImmediateProgram
        {
            public int[] Zones;
            public string Name;

            public bool IsEmpty
            {
                get
                {
                    return Zones.All(z => z <= 0);
                }
            }
        }

        public GardenDevice(double counterFq = 5.5)
        {
            _counterFq = counterFq;
            _timeProgram = new TimeProgram<GardenCycle>(Logger);

            var cfgColder = Manager.GetService<PersistenceService>().GetAppFolderPath("Server");
            _cfgFile = new FileInfo(Path.Combine(cfgColder, "gardenCfg.json"));
            if (!_cfgFile.Exists)
            {
                using (var stream = _cfgFile.OpenWrite())
                {
                    // No data. Write the current settings
                    new DataContractJsonSerializer(typeof(Configuration)).WriteObject(stream, new Configuration { Program = TimeProgram<GardenCycle>.DefaultProgram });
                }
            }

            _timeProgram.CycleTriggered += HandleProgramCycle;
            ReadConfig();

            // Prepare CSV file
            var dbFolder = new DirectoryInfo(Manager.GetService<PersistenceService>().GetAppFolderPath("Db/GARDEN"));
            _csvFile = new FileInfo(Path.Combine(dbFolder.FullName, "garden.csv"));
            if (!_csvFile.Exists)
            {
                CsvHelper<GardenCsvRecord>.WriteCsvHeader(_csvFile);
            }

            // Subscribe changes
            var cfgFileObserver = new FileSystemWatcher(cfgColder, "gardenCfg.json");
            cfgFileObserver.Changed += (o, e) => Debounce(() => ReadConfig());
            cfgFileObserver.NotifyFilter = NotifyFilters.LastWrite;
            cfgFileObserver.EnableRaisingEvents = true;

            // To receive commands from UI
            Manager.GetService<PipeServer>().Message += async (o, e) =>
            {
                switch (e.Request.Command)
                {
                    case "garden.getStatus":
                        e.Response = Task.Run(async () =>
                        {
                            FlowData flowData = await ReadFlow();
                            return (WebResponse) new GardenWebResponse { Online = GetFirstOnlineSink<GardenSink>() != null, Configuration = new Configuration { Program = _timeProgram.Program, ZoneNames = _zoneNames }, FlowData = flowData };
                        });
                        break;
                    case "garden.setImmediate":
                        Logger.Log("setImmediate", "zones", string.Join(",", e.Request.ImmediateZones));
                        e.Response = Task.FromResult((WebResponse) new GardenWebResponse { Error = ScheduleCycle(new ImmediateProgram { Zones = e.Request.ImmediateZones, Name = "Immediate" } ) });
                        break;
                    case "garden.stop":
                        bool stopped = false;
                        foreach (var sink in Sinks.OfType<GardenSink>())
                        {
                            sink.ResetNode();
                            stopped = true;
                        }
                        string error = null;
                        if (!stopped)
                        {
                            error = "Cannot stop, no sink";
                        }
                        e.Response = Task.FromResult((WebResponse) new GardenWebResponse { Error = error });
                        break;
                }
            };

            StartLoop();
        }

        private async Task<FlowData> ReadFlow()
        {
            var flowSink = GetFirstOnlineSink<FlowSink>();
            if (flowSink != null)
            {
                try
                {
                    return await flowSink.ReadData(_counterFq);
                }
                catch (Exception exc)
                {
                    Logger.Exception(exc);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Persistent loop
        /// </summary>
        /// <returns></returns>
        private async Task StartLoop()
        {
            StepActions lastStepActions = null;
            bool inProgress = false;
            int errors = 0;

            while (!IsDisposed)
            {
                // Program in progress?
                bool cycleIsWaiting = false;
                // Check for new program to run
                lock (_cycleQueue)
                {
                    cycleIsWaiting = _cycleQueue.Count > 0;
                }

                // Do I need to contact the garden sink?
                if (inProgress || cycleIsWaiting)
                {
                    var gardenSink = GetFirstOnlineSink<GardenSink>();
                    if (gardenSink != null)
                    {
                        errors = 0;

                        // Wait for a free garden device
                        var state = await gardenSink.Read(false);
                        DateTime now = DateTime.Now;
                        if (state.IsAvailable)
                        {
                            // Finished?
                            if (lastStepActions != null)
                            {
                                lastStepActions.StopAction(now);
                                lastStepActions = null;
                                inProgress = false;
                            }

                            // New program to load?
                            if (cycleIsWaiting)
                            {
                                ImmediateProgram cycle;
                                int[] zones = null;
                                lock (_cycleQueue)
                                {
                                    cycle = _cycleQueue.Dequeue();
                                    zones = (int[])cycle.Zones.Clone();
                                }
                                await gardenSink.WriteProgram(zones);
                                inProgress = true;
                                lastStepActions = await LogStartProgram(now, cycle);
                            }
                        }
                        else
                        {
                            if (inProgress)
                            {
                                // The program is running? Log flow
                                await lastStepActions.StepAction(now, state);
                            }
                        }
                    }
                    else
                    {
                        // Lost connection with garden programmer!
                        if (errors++ < 5)
                        {
                            Logger.Log("Cannot contact garden", "cycleIsWaiting", cycleIsWaiting, "inProgress", inProgress);
                        }
                    }
                }

                await Task.Delay(POLL_PERIOD);
            }
        }

        protected override Task OnTerminate()
        {
            lock (_timeProgramLock)
            {
                _timeProgram.Dispose();
            }
            return base.OnTerminate();
        }

        /// <summary>
        /// Used to read config when the FS notifies changes
        /// </summary>
        private void Debounce(Action handler)
        {
            // Event comes two time (the first one with an empty file)
            if (_debounceTimer == null)
            {
                _debounceTimer = new Timer(o => 
                {
                    _debounceTimer = null;
                    handler();
                }, null, 1000, Timeout.Infinite);
            }
        }

        /// <summary>
        /// JSON for configuration serialization
        /// </summary>
        [DataContract]
        public class Configuration
        {
            [DataMember(Name = "program")]
            public TimeProgram<GardenCycle>.ProgramData Program { get; set; }

            [DataMember(Name = "zones")]
            public string[] ZoneNames { get; set; }
        }

        private void ReadConfig()
        {
            lock (_timeProgramLock)
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Configuration));
                Configuration configuration;
                try
                {
                    using (var stream = File.Open(_cfgFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        configuration = serializer.ReadObject(stream) as Configuration;
                    }
                }
                catch (Exception exc)
                {
                    Logger.Log("Cannot read garden configuration", "Exc", exc.Message);
                    return;
                }

                // Apply configuration
                Logger.Log("New configuration acquired", "cycles#", configuration?.Program?.Cycles?.Length);

                try
                {
                    _timeProgram.SetProgram(configuration.Program);
                    _zoneNames = configuration.ZoneNames ?? new string[0];
                }
                catch (ArgumentException exc)
                {
                    // Error applying configuration
                    Logger.Log("CONFIGURATION ERROR", "exc", exc.Message);
                }
            }
        }

        private void HandleProgramCycle(object sender, TimeProgram<GardenCycle>.CycleTriggeredEventArgs e)
        {
            Logger.Log("ScheduleProgram", "name", e.Cycle.Name);
            ScheduleCycle(new ImmediateProgram { Zones = e.Cycle.Zones, Name = e.Cycle.Name } );
        }

        private string ScheduleCycle(ImmediateProgram program)
        {
            if (!program.IsEmpty)
            {
                lock (_cycleQueue)
                {
                    _cycleQueue.Enqueue(program);
                }
                return null;
            }
            else
            {
                return "Empty program";
            }
        }

        private class StepActions
        {
            public Action<DateTime> StopAction;
            public Func<DateTime, GardenSink.TimerState, Task> StepAction;
        }

        private async Task<StepActions> LogStartProgram(DateTime now, ImmediateProgram cycle)
        {
            Logger.Log("Garden", "cycle start", cycle.Name);

            GardenCsvRecord data = new GardenCsvRecord
            {
                Date = now.Date,
                Time = now.TimeOfDay,
                Cycle = cycle.Name,
                Zones = string.Join(";", cycle.Zones.Select(t => t.ToString())),
                State = 1,
            };

            double startQty = 0;

            var flowData = (await ReadFlow());
            if (flowData != null)
            {
                startQty = data.TotalQtyMc = flowData.TotalMc;
            }

            lock (_csvFile)
            {
                CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, data);
            }

            // Return the action to log the stop program
            Action<DateTime> stopAction = async now1 =>
            {
                Logger.Log("Garden", "cycle end", cycle.Name);

                if (startQty > 0)
                {
                    var flowData1 = (await ReadFlow());
                    if (flowData1 != null)
                    {
                        data.QtyL = (flowData1.TotalMc - startQty) * 1000.0;
                        data.TotalQtyMc = flowData1.TotalMc;
                        data.FlowLMin = flowData1.FlowLMin;
                    }
                }

                data.State = 0;
                data.Date = now1.Date;
                data.Time = now1.TimeOfDay;
                lock (_csvFile)
                {
                    CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, data);
                }

                ScheduleMail(now, cycle);
            };

            // Log a flow info
            Func<DateTime, GardenSink.TimerState, Task> stepAction = async (now1, state) =>
            {
                if (startQty > 0)
                {
                    var flowData1 = (await ReadFlow());
                    if (flowData1 != null)
                    {
                        data.State = 2;
                        data.Date = now1.Date;
                        data.Time = now1.TimeOfDay;
                        data.QtyL = (flowData1.TotalMc - startQty) * 1000.0;
                        data.TotalQtyMc = flowData1.TotalMc;
                        data.FlowLMin = flowData1.FlowLMin;
                        data.Zones = string.Join(";", state.ZoneRemTimes.Select(t => t.ToString()));
                        lock (_csvFile)
                        {
                            CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, data);
                        }
                    }
                }
            };

            return new StepActions { StopAction = stopAction, StepAction = stepAction };
        }

        private List<MailData> _mailData = new List<MailData>();
        private class MailData
        {
            public string Name;
            public Tuple<string, int>[] ZoneData;
        }

        private void ScheduleMail(DateTime now, ImmediateProgram cycle)
        {
            _mailData.Add(new MailData
            {
                Name = cycle.Name,
                ZoneData = cycle.Zones.Select((time, i) => Tuple.Create(time, i)).Where(t => t.Item1 > 0).Select(tuple =>
                {
                    return Tuple.Create(GetZoneName(tuple.Item2), tuple.Item1);
                }).ToArray()
            });

            bool sendNow = true;
            // If more programs will follow, don't send the mail now
            lock (_timeProgramLock)
            {
                var nextCycle = _timeProgram.GetNextCycles(now, 1);
                if (nextCycle.Length > 0 && nextCycle[0] < now + TimeSpan.FromMinutes(5))
                {
                    // Don't send
                    sendNow = false;
                }
            }

            if (sendNow)
            {
                // Schedule mail
                string body = "Programmi:" + Environment.NewLine;
                body += string.Join(Environment.NewLine, _mailData.Select(data => data.Name + Environment.NewLine + string.Join(Environment.NewLine, data.ZoneData.Select(t =>
                {
                    return string.Format("{0}: {1} minuti", t.Item1, t.Item2);
                }))));

                Manager.GetService<INotificationService>().SendMail("Giardino irrigato", body);
            }
        }

        private string GetZoneName(int index)
        {
            if (index < _zoneNames.Length)
            {
                return _zoneNames[index];
            }
            else
            {
                return index.ToString();
            }
        }
    }
}

