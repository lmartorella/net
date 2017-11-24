using System.Linq;
using Lucky.Home.Sinks;
using System;
using System.Threading;
using Lucky.Services;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace Lucky.Home.Devices
{
    [Device("Garden")]
    [Requires(typeof(GardenSink))]
    public class GardenDevice : DeviceBase
    {
        private Timer _timer;
        private FileInfo _cfgFile;
        private Timer _debounceTimer;

        public GardenDevice()
        {
            var folder = Manager.GetService<PersistenceService>().GetAppFolderPath("Server");
            _cfgFile = new FileInfo(Path.Combine(folder, "gardenCfg.json"));
            if (!_cfgFile.Exists)
            {
                using (var stream = _cfgFile.OpenWrite())
                {
                    using (var writer = new StreamWriter(stream))
                    {
                        // No data
                        writer.Write("{ }");
                    }
                }
            }
            ReadConfig();

            // Subscribe changes
            var cfgFIleObserver = new FileSystemWatcher(folder, "gardenCfg.json");
            cfgFIleObserver.Changed += (o, e) => Debounce(() => ReadConfig());
            cfgFIleObserver.NotifyFilter = NotifyFilters.LastWrite;
            cfgFIleObserver.EnableRaisingEvents = true;

            //_timer = new Timer(o =>
            //{
            //    if (IsFullOnline)
            //    {
            //        var sink = Sinks.OfType<GardenSink>().First();
            //        bool isAval = sink.Read();
            //        if (isAval)
            //        {
            //            sink.WriteProgram(new int[] { 0, 0, 0, 1 });
            //        }
            //    }
            //}, null, 0, 1000);
        }

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
        /// Deserialize JSON
        /// </summary>
        [DataContract]
        public class Configuration
        {
            [DataContract]
            public class Program
            {
                [DataMember]
                public string startTime;
                [DataMember]
                public int[] daysOfWeek;
            }

            [DataMember]
            public Program[] programs;
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
            Logger.Log("New configuration acquired", "program#", configuration.programs.Length);
        }
    }
}

