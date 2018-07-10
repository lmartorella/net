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

        [Csv]
        public int Start;
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
        private readonly TimeProgram<GardenCycle> _timeProgram = new TimeProgram<GardenCycle>();
        private readonly Queue<ImmediateProgram> _cycleQueue = new Queue<ImmediateProgram>();
        private readonly FileInfo _csvFile;
        private Action _lastLogForStop;
        private string[] _zoneNames = new string[0];

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

        public GardenDevice()
        {
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
            var cfgFIleObserver = new FileSystemWatcher(cfgColder, "gardenCfg.json");
            cfgFIleObserver.Changed += (o, e) => Debounce(() => ReadConfig());
            cfgFIleObserver.NotifyFilter = NotifyFilters.LastWrite;
            cfgFIleObserver.EnableRaisingEvents = true;

            // To receive commands from UI
            Manager.GetService<PipeServer>().Message += async (o, e) =>
            {
                switch (e.Request.Command)
                {
                    case "garden.getStatus":
                        e.Response = Task.Run(async () =>
                        {
                            var flowSink = Sinks.OfType<FlowSink>().FirstOrDefault(s => s.IsOnline);
                            FlowData flowData = null;
                            if (flowSink != null)
                            {
                                flowData = await flowSink.ReadData(5.5);
                            }
                            return new WebResponse { Online = IsFullOnline, Configuration = new Configuration { Program = _timeProgram.Program, ZoneNames = _zoneNames }, FlowData = flowData };
                        });
                        break;
                    case "garden.setImmediate":
                        Logger.Log("setImmediate", "zones", e.Request.ImmediateZones);
                        e.Response = Task.FromResult(new WebResponse { Error = ScheduleCycle(new ImmediateProgram { Zones = e.Request.ImmediateZones, Name = "Immediate" } ) });
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
                        e.Response = Task.FromResult(new WebResponse { Error = error });
                        break;
                }
            };

            StartLoop();
        }

        private async Task StartLoop()
        { 
            while (!IsDisposed)
            {
                await HandlePollTimer();
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

        private async Task HandlePollTimer()
        {
            if (IsFullOnline)
            {
                var sink = Sinks.OfType<GardenSink>().First();
                // Wait for a free garden device
                // Write fancy logs
                bool isAvail = await sink.Read(false);
                if (isAvail)
                {
                    int[] zones = null;
                    lock (_cycleQueue)
                    {
                        if (_lastLogForStop != null)
                        {
                            _lastLogForStop();
                            _lastLogForStop = null;
                        }

                        if (_cycleQueue.Count > 0)
                        {
                            var cycle = _cycleQueue.Dequeue();
                            zones = cycle.Zones;
                            _lastLogForStop = LogStartProgram(cycle);
                        }
                    }
                    if (zones != null)
                    {
                        await sink.WriteProgram(zones);
                    }
                }
            }
        }

        private Action LogStartProgram(ImmediateProgram cycle)
        {
            Logger.Log("Garden", "cycle start", cycle.Name);

            var now = DateTime.Now;
            GardenCsvRecord data = new GardenCsvRecord
            {
                Date = now.Date,
                Time = now.TimeOfDay,
                Cycle = cycle.Name,
                Zones = string.Join(";", cycle.Zones.Select(t => t.ToString())),
                Start = 1
            };
            lock (_csvFile)
            {
                CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, data);
            }

            // Return the action to log the stop program
            return () =>
            {
                Logger.Log("Garden", "cycle end", cycle.Name);
                data.Start = 0;
                lock (_csvFile)
                {
                    CsvHelper<GardenCsvRecord>.WriteCsvLine(_csvFile, data);
                }

                // Send mail
                string body = "Zone irrigate:" + Environment.NewLine + string.Join(Environment.NewLine, cycle.Zones.Select((z, i) => Tuple.Create(z, i)).Where(t => t.Item1 > 0).Select(t =>
                {
                    return string.Format("{0}: {1} minuti", GetZoneName(t.Item2), t.Item1);
                }));
                Manager.GetService<INotificationService>().SendMail("Giardino innaffiato", body);
            };
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

