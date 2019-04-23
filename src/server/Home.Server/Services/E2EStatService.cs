using Lucky.Db;
using Lucky.Home.Services;
using System;
using System.IO;
using System.Threading;

namespace Lucky.Home.Services
{
    /// <summary>
    /// End-to-end statistics service
    /// </summary>
    class E2EStatService : IService
    {
        private Timer _timer;
        private readonly FileInfo _csvFile;

        public E2EStatService()
        {
            var folder = new DirectoryInfo(Manager.GetService<PersistenceService>().GetAppFolderPath("Stats"));
            _csvFile = new FileInfo(Path.Combine(folder.FullName, "e2e.csv"));

            // Minute timer
            _timer = new Timer((o) =>
            {
                Tick?.Invoke(this, EventArgs.Empty);
            }, null, 0, 60000);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public event EventHandler Tick;

        private class E2ERecord
        {
            [Csv("yyyy-MM-dd HH:mm:ss")]
            public DateTime TimeStamp;

            [Csv]
            public string NodeId;

            [Csv("0.0")]
            public double Min;

            [Csv("0.0")]
            public double Max;

            [Csv("0.0")]
            public double Average;
        }

        internal void Report(DateTime timeStamp, string nodeId, double min, double max, double average)
        {
            CsvHelper<E2ERecord>.WriteCsvLine(_csvFile, new E2ERecord { TimeStamp = timeStamp, NodeId = nodeId, Min = min, Max = max, Average = average });
        }
    }
}
