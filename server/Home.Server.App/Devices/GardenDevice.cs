using System.Linq;
using Lucky.Home.Sinks;
using System;
using System.Threading;
using Lucky.Services;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Lucky.Net;
using System.Threading.Tasks;
using Lucky.Home.Model;
using System.Collections.Generic;

namespace Lucky.Home.Devices
{
    [Device("Garden")]
    [Requires(typeof(GardenSink))]
    public class GardenDevice : DeviceBase
    {
        private static int POLL_PERIOD = 3000;
        private Timer _pollTimer;
        private FileInfo _cfgFile;
        private Timer _debounceTimer;
        private object _timeProgramLock = new object();
        private TimeProgram<GardenCycle> _timeProgram;
        private readonly Queue<int[]> _cycleQueue = new Queue<int[]>();

        [DataContract]
        public class GardenCycle : TimeProgram<GardenCycle>.Cycle
        {
            [DataMember]
            public int[] Zones;
        }

        public GardenDevice()
        {
            var folder = Manager.GetService<PersistenceService>().GetAppFolderPath("Server");
            _cfgFile = new FileInfo(Path.Combine(folder, "gardenCfg.json"));
            if (!_cfgFile.Exists)
            {
                using (var stream = _cfgFile.OpenWrite())
                {
                    // No data. Write the current settings
                    new DataContractJsonSerializer(typeof(Configuration)).WriteObject(stream, new Configuration { Program = TimeProgram<GardenCycle>.DefaultProgram });
                }
            }
            ReadConfig();

            // Subscribe changes
            var cfgFIleObserver = new FileSystemWatcher(folder, "gardenCfg.json");
            cfgFIleObserver.Changed += (o, e) => Debounce(() => ReadConfig());
            cfgFIleObserver.NotifyFilter = NotifyFilters.LastWrite;
            cfgFIleObserver.EnableRaisingEvents = true;

            // To receive commands from UI
            StartNamedPipe();
        }

        protected override void Dispose(bool disposing)
        {
            lock (_timeProgramLock)
            {
                if (_timeProgram != null)
                {
                    _timeProgram.Dispose();
                    _timeProgram = null;
                }
                if (_pollTimer != null)
                {
                    _pollTimer.Dispose();
                }
                _pollTimer = null;
            }
            base.Dispose(disposing);
        }

        [DataContract]
        public class WebRequest
        {
            /// <summary>
            /// Can be getProgram, setImmediate
            /// </summary>
            [DataMember(Name = "command")]
            public string Command { get; set; }

            [DataMember(Name = "immediate")]
            public int[] ImmediateZones { get; set; }
        }

        [DataContract]
        public class WebResponse
        {
            /// <summary>
            /// List of programs (if requested)
            /// </summary>
            [DataMember(Name = "program")]
            public TimeProgram<GardenCycle>.ProgramData Program { get; set; }
        }

        private async void StartNamedPipe()
        {
            var server = new PipeJsonServer<WebRequest, WebResponse>("NETGARDEN");
            server.ManageRequest = req => 
            {
                var resp = new WebResponse();
                switch (req.Command)
                {
                    case "getProgram":
                        lock (_timeProgramLock)
                        {
                            resp.Program = _timeProgram.Program;
                        }
                        break;
                    case "setImmediate":
                        ScheduleCycle(req.ImmediateZones);
                        break;
                }
                return Task.FromResult(resp);
            };
            await server.Start();
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

            [DataMember(Name = "zoneMd")]
            public ZoneMd[] ZoneMd { get; set; }
        }

        /// <summary>
        /// Metadata descriptor for each zone
        /// </summary>
        public class ZoneMd
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }
        }

        private void ReadConfig()
        {
            lock (_timeProgramLock)
            {
                if (_timeProgram != null)
                {
                    _timeProgram.CycleTriggered -= HandleProgramCycle;
                }

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
                Logger.Log("New configuration acquired", "program#", configuration?.Program?.Cycles?.Length);

                if (_timeProgram == null)
                {
                    _timeProgram = new TimeProgram<GardenCycle>(configuration.Program);
                }
                else
                {
                    _timeProgram.Program = configuration.Program;
                }

                _timeProgram.CycleTriggered += HandleProgramCycle;
            }
        }

        private void HandleProgramCycle(object sender, TimeProgram<GardenCycle>.CycleTriggeredEventArgs e)
        {
            ScheduleCycle(e.Cycle.Zones);
        }

        private void ScheduleCycle(int[] zones)
        {
            lock (_cycleQueue)
            {
                _cycleQueue.Enqueue(zones);
            }
            StartPollTimer();
        }

        private void StartPollTimer()
        {
            if (_pollTimer == null)
            {
                _pollTimer = new Timer(o =>
                {
                    if (IsFullOnline)
                    {
                        var sink = Sinks.OfType<GardenSink>().First();
                        // Wait for a free garden device
                        // Write fancy logs
                        bool isAval = sink.Read();
                        if (isAval)
                        {
                            lock (_cycleQueue)
                            {
                                if (_cycleQueue.Count == 0)
                                {
                                    StopPollTimer();
                                }
                                else
                                {
                                    var cycle = _cycleQueue.Dequeue();
                                    sink.WriteProgram(cycle);
                                    Console.WriteLine("== GARDEN RUN: Times: {0}", string.Join(", ", cycle.Select(t => t.ToString())));
                                }
                            }
                        }
                    }
                }, null, 0, POLL_PERIOD);
            }
        }

        private void StopPollTimer()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Dispose();
                _pollTimer = null;
            }
        }
    }
}

