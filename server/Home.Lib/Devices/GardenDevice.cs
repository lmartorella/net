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

namespace Lucky.Home.Devices
{
    [Device("Garden")]
    [Requires(typeof(GardenSink))]
    public class GardenDevice : DeviceBase
    {
        private Timer _timer;
        private FileInfo _cfgFile;
        private Timer _debounceTimer;
        private TimeProgram<GardenCycle> _timeProgram;

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

            _timer = new Timer(o =>
            {
                if (IsFullOnline)
                {
                    var sink = Sinks.OfType<GardenSink>().First();
                    bool isAval = sink.Read();
                    if (isAval)
                    {
                        sink.WriteProgram(new int[] { 0, 0, 0, 1 });
                    }
                }
            }, null, 0, 3000);

            StartNamedPipe();
        }

        [DataContract]
        public class WebRequest
        {
            [DataMember(Name = "getProgram")]
            public bool GetProgram { get; set; }
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
                if (req.GetProgram)
                {
                    resp.Program = _timeProgram.Program;
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
        }
    }
}

