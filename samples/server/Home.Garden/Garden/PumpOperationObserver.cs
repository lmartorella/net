using Lucky.Home.Services;
using System;
using System.IO;

namespace Lucky.Home.Devices.Garden
{
    class PumpOperationObserver
    {
        private FileInfo _pumpFile;
        private readonly DigitalInputArrayRpc _pumpSink;
        private int _pumpSubIndex;

        public PumpOperationObserver(DigitalInputArrayRpc pumpSink)
        {
            _pumpSink = pumpSink;
            var dbFolder = new DirectoryInfo(Manager.GetService<PersistenceService>().GetAppFolderPath("Db/GARDEN"));
            _pumpFile = new FileInfo(Path.Combine(dbFolder.FullName, "pump.log"));
            Log("{0:u} Started", DateTime.Now);
            _pumpSink.EventReceived += HandlePumpSinkData;
        }

        private void HandlePumpSinkData(object sender, DigitalInputArrayRpc.EventReceivedEventArgs e)
        {
            if (e.SubIndex == _pumpSubIndex)
            {
                // Log change
                // 220v sense is inverted bool
                Log("{0:u} Pump {1}", e.Timestamp, e.State ? "OFF" : "ON");
            }
        }

        private void Log(string format, params object[] args)
        {
            using (var writer = new StreamWriter(_pumpFile.FullName, true))
            {
                writer.WriteLine(format, args);
            }
        }
    }
}
