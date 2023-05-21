using Lucky.Home.IO;
using Lucky.Home.Model;
using Lucky.Home.Services;
using Lucky.Home.Sinks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Devices.Garden
{
    /// <summary>
    /// Control and diagnose a custom garden programmer
    /// </summary>
    internal class GardenDevice
    {
        /// <summary>
        /// Poll for the main cycle
        /// </summary>
        private static TimeSpan MAIN_POLL_PERIOD = TimeSpan.FromSeconds(3);

        private FileInfo _cfgFile;
        private readonly TimeProgram<GardenCycle> _timeProgram;
        private readonly Queue<ZoneTime> _immediateQueue = new Queue<ZoneTime>();
        private Configuration _configuration = new Configuration { Program = TimeProgram<GardenCycle>.DefaultProgram };
        private FileWatcher _fileWatcher;
        private readonly MailScheduler _executedCyclesMailScheduler;
        private readonly MailScheduler _suspendedCyclesMailScheduler;
        private readonly PumpOperationObserver _pumpOpObserver;
        private RunningProgram _runningProgram;
        private readonly ILogger Logger;
        private readonly FlowRpc FlowSink;
        private readonly GardenRpc GardenSink;
        private readonly DigitalInputArrayRpc PumpSink;
        private bool IsDisposed = false;

        public bool InUse { get; private set; }

        public GardenDevice(GardenRpc gardenSink, FlowRpc flowSink, DigitalInputArrayRpc pumpSink)
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("Garden");
            FlowSink = flowSink;
            GardenSink = gardenSink;
            PumpSink = pumpSink;
            _pumpOpObserver = new PumpOperationObserver(pumpSink);

            _timeProgram = new TimeProgram<GardenCycle>(Logger);

            _executedCyclesMailScheduler = new MailScheduler(this, Resources.gardenMailTitle, Resources.gardenMailHeader);
            _suspendedCyclesMailScheduler = new MailScheduler(this, Resources.gardenMailSuspendedTitle, Resources.gardenMailSuspendedHeader);

            _timeProgram.CycleTriggered += (_, e) =>
                {
                    int index = Array.IndexOf(_timeProgram.Program.Cycles, e.Item);
                    Logger.Log("ScheduleProgram", "idx", index, "suspended", e.Item.Suspended);
                    if (e.Item.Suspended)
                    {
                        // Send a mail after the duration, to let the mail scheduler to coalesce mails
                        _suspendedCyclesMailScheduler.ScheduleSuspendedMail(DateTime.Now + e.Item.NomimalDuration, _configuration.GetCycleName(e.Item));
                    }
                    else
                    {
                        ScheduleCycle(new ZoneTime { Minutes = e.Item.Minutes, Zones = e.Item.Zones });
                    }
                };

            var cfgColder = Manager.GetService<PersistenceService>().GetAppFolderPath("server");
            _cfgFile = new FileInfo(Path.Combine(cfgColder, "gardenCfg.json"));
            if (!_cfgFile.Exists)
            {
                using (var stream = _cfgFile.Open(FileMode.Create))
                {
                    // No data. Write the current settings
                    new DataContractJsonSerializer(typeof(Configuration)).WriteObject(stream, _configuration);
                }
            }
            // Subscribe changes
            _fileWatcher = new FileWatcher(_cfgFile);
            _fileWatcher.Changed += (_o, _e) => ReadConfig();
            ReadConfig();

            // To receive commands from UI
            var mqttClient = Manager.GetService<MqttService>();
            _ = mqttClient.SubscribeJsonRpc("garden/getStatus", async (RpcVoid request) =>
            {
                FlowData flowData = await ReadFlow();

                // Tactical
                if (flowData != null)
                {
                    await (GardenSink.UpdateFlowData((int)flowData.FlowLMin) ?? Task.FromResult<string>(null));
                }

                NextCycle[] nextCycles;
                lock (_immediateQueue)
                {
                    lock (_timeProgram)
                    {
                        // Concat running program, immediate queue and then scheduled
                        nextCycles = (_runningProgram != null ? new[] { _runningProgram.ToNextCycle(_configuration) } : new NextCycle[0])
                            .Concat(_immediateQueue.Select(cycle => new NextCycle(cycle, _configuration, false)))
                            .Concat(_timeProgram?.GetNextCycles(DateTime.Now).Select(c => new NextCycle(c.Item1, _configuration, c.Item2)))
                            .Take(4).ToArray();
                    }
                }

                return new GardenStatusRpcResponse
                {
                    Status = OnlineStatus,
                    Configuration = _configuration,
                    FlowData = flowData,
                    NextCycles = nextCycles,
                    IsRunning = _runningProgram != null
                };
            });
            _ = mqttClient.SubscribeJsonRpc("garden/setImmediate", (GardenSetImmediateRpcRequest request) =>
            {
                var zones = request.ImmediateZone;
                Logger.Log("setImmediate", "msg", zones.ToString());
                ScheduleCycle(new ZoneTime
                {
                    Minutes = zones.Time,
                    Zones = zones.Zones
                });
                return Task.FromResult(new RpcVoid());
            });
            _ = mqttClient.SubscribeJsonRpc("garden/stop", async (RpcVoid request) =>
            {
                bool stopped = await GardenSink.ResetNode();
                if (!stopped)
                {
                    throw new ArgumentException("Cannot stop, no sink");
                }
                return new RpcVoid();
            });
            _ = mqttClient.SubscribeJsonRpc("garden/setConfig", async (GardenSetConfigRpcRequest request) =>
            {
                // Set new config value. Verify it before accepting it.
                var config = request.Configuration;
                // This will raise exception if data is invalid
                AcquireConfiguration(config);
                // Save back data
                await SaveConfiguration(config);
                return new RpcVoid();
            });
        }

        internal async Task<FlowData> ReadFlow()
        {
            var flowSink = FlowSink;
            if (flowSink != null)
            {
                try
                {
                    return await flowSink.ReadData();
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

        private OnlineStatus OnlineStatus
        {
            get
            {
                var sinks = new BaseRpc[] { GardenSink, FlowSink, PumpSink };
                if (sinks.All(s => s.IsOnline))
                {
                    return OnlineStatus.Online;
                }
                if (sinks.All(s => !s.IsOnline))
                {
                    return OnlineStatus.Offline;
                }
                return OnlineStatus.PartiallyOnline;
            }
        }

        /// <summary>
        /// Persistent loop
        /// </summary>
        public async Task StartLoop()
        {
            int errors = 0;

            while (!IsDisposed)
            {
                await Task.Delay(MAIN_POLL_PERIOD);

                // Program in progress?
                bool cycleIsWaiting = false;
                // Check for new program to run
                lock (_immediateQueue)
                {
                    cycleIsWaiting = _immediateQueue.Count > 0;
                }

                // Do I need to contact the garden sink?
                InUse = _runningProgram != null || cycleIsWaiting;
                if (InUse)
                {
                    var gardenSink = GardenSink;
                    if (gardenSink != null)
                    {
                        // Wait for a free garden device
                        var state = await gardenSink.ReadState();
                        if (state == null)
                        {
                            // Lost connection with garden programmer!
                            if (errors++ < 5)
                            {
                                Logger.Log("Cannot contact garden (2)", "cycleIsWaiting", cycleIsWaiting, "inProgress", _runningProgram != null);
                                continue;
                            }
                        }

                        errors = 0;
                        DateTime now = DateTime.Now;
                        if (state.IsAvailable)
                        {
                            // Finished?
                            if (_runningProgram != null)
                            {
                                InUse = false;
                                _ = _runningProgram.Stop(now);
                                _runningProgram = null;
                            }

                            // New program to load?
                            if (cycleIsWaiting)
                            {
                                ZoneTime cycle;
                                GardenRpc.ImmediateZoneTime zoneTimes = null;
                                lock (_immediateQueue)
                                {
                                    cycle = _immediateQueue.Dequeue();
                                    zoneTimes = new GardenRpc.ImmediateZoneTime { Minutes = cycle.Minutes, ZoneMask = ToZoneMask(cycle.Zones) };
                                }
                                await gardenSink.WriteProgram(new[] { zoneTimes });

                                _runningProgram = new RunningProgram(cycle, Logger, this, _configuration.GetCycleName(cycle));
                                await _runningProgram.Start(now);
                            }
                        }
                        else
                        {
                            if (_runningProgram != null)
                            {
                                // The program is running? Log flow
                                await _runningProgram.Step(now, state);
                            }
                        }
                    }
                    else
                    {
                        // Lost connection with garden programmer!
                        if (errors++ < 5)
                        {
                            Logger.Log("Cannot contact garden", "cycleIsWaiting", cycleIsWaiting, "inProgress", _runningProgram != null);
                        }
                    }
                }
            }
        }

        public void OnTerminate()
        {
            IsDisposed = true;
            lock (_timeProgram)
            {
                _timeProgram.Dispose();
            }
            _fileWatcher.Dispose();
        }

        private Configuration TryParseConfigurationJson(string context, Stream stream, out string error)
        {
            error = null;
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Configuration));
            try
            {
                using (stream)
                {
                    return serializer.ReadObject(stream) as Configuration;
                }
            }
            catch (Exception exc)
            {
                error = exc.Message;
                Logger.Log("Cannot read garden configuration", "Context", context, "Exc", error);
                return null;
            }
        }

        /// <summary>
        /// Save back configuration, with file watcher disabled
        /// </summary>
        private Task SaveConfiguration(Configuration configuration)
        {
            // Disable watcher
            Task watcher = _fileWatcher.SuspendAndWaitForUpdate();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(
                typeof(Configuration), 
                new DataContractJsonSerializerSettings 
                { 
                    
                }
            );

            using (var stream = File.Open(_cfgFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, true, "   "))
                {
                    serializer.WriteObject(writer, configuration);
                }
            }
            // Wait for the watcher to detect the change and re-engage again
            return watcher;
        }

        private void ReadConfig()
        {
            // Try read and check for errors
            string error;
            var configuration = TryParseConfigurationJson("watcher", File.Open(_cfgFile.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), out error);
            if (error == null)
            {
                AcquireConfiguration(configuration);
            }
        }

        private void AcquireConfiguration(Configuration configuration)
        {
            lock (_timeProgram)
            {
                try
                {
                    _timeProgram.SetProgram(configuration.Program);
                    _configuration = configuration;

                    // Apply configuration
                    Logger.Log("New configuration acquired", "cycles#", configuration.Program?.Cycles?.Length);
                }
                catch (ArgumentException exc)
                {
                    // Error applying configuration
                    Logger.Log("Config Validation Error", "exc", exc.Message);
                }
            }
        }

        private void ScheduleCycle(ZoneTime program)
        {
            if (program.Minutes > 0)
            {
                lock (_immediateQueue)
                {
                    _immediateQueue.Enqueue(program);
                }
            }
            else
            {
                throw new ArgumentException("Empty program");
            }
        }

        internal static byte ToZoneMask(int[] zones)
        {
            byte ret = 0;
            foreach (int zone in zones)
            {
                ret = (byte)(ret | (1 << zone));
            }
            return ret;
        }

        internal Tuple<GardenCycle, DateTime> GetNextCycle(DateTime now)
        {
            lock (_timeProgram)
            {
                return _timeProgram.GetNextCycles(now).FirstOrDefault();
            }
        }

        internal void ScheduleMail(DateTime now, string name, int quantityL, int minutes)
        {
            _executedCyclesMailScheduler.ScheduleMail(now, name, quantityL, minutes);
        }
    }
}

