using Lucky.Services;
using System;
using System.IO;

namespace Lucky.Home.Db
{
    interface IFsTimeSeries
    {
        void Rotate(string fileName, DateTime start);
    }

    class FsTimeSeries<T> : IFsTimeSeries, ITimeSeries<T> where T : ISupportAverage<T>, ISupportCsv, new()
    {
        private string _folder;
        private FileInfo _fileName;

        private PeriodData<T> _currentPeriod;
        private PeriodData<T> _lastPeriod;
        private string _timeStampFormat;
        private string _header;
        private ILogger _logger;

        public FsTimeSeries(string folderPath, string timeStampFormat)
        {
            _logger = Manager.GetService<ILoggerFactory>().Create("Db/" + folderPath);
            _timeStampFormat = timeStampFormat;
            _folder = Manager.GetService<PersistenceService>().GetAppFolderPath("Db/" + folderPath);
            _fileName = new FileInfo("nul");
            _header = new T().CsvHeader;

            _logger.Log("Started");
        }

        public void Rotate(string fileName, DateTime start)
        {
            lock (_fileName)
            {
                _fileName = new FileInfo(Path.Combine(_folder, fileName));
                _lastPeriod = _currentPeriod;
                _currentPeriod = new PeriodData<T>(start);

                // Write CSV header
                WriteLine(writer => writer.WriteLine("TimeStamp," + _header));
                _logger.Log("Rotated", "date", start.Date);
            }
        }

        public T LastData
        {
            get
            {
                return _currentPeriod.LastSample;
            }
        }

        public Aggregation<T> CurrentPeriodData
        {
            get
            {
                return _currentPeriod.GetAggregation(DateTime.Now);
            }
        }

        public Aggregation<T> LastPeriodData
        {
            get
            {
                return _lastPeriod.GetAggregation(_currentPeriod.Begin);
            }
        }

        public Aggregation<T> FromCustomPeriod(DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }

        private void WriteLine(Action<StreamWriter> handler)
        {
            using (var stream = _fileName.Open(FileMode.Append, FileAccess.Write, FileShare.Read))
            {
                using (var writer = new StreamWriter(stream))
                {
                    handler(writer);
                }
            }
        }

        public void AddNewSample(T sample, DateTime ts)
        {
            lock (_fileName)
            {
                _currentPeriod.Add(sample, ts);

                // In addition write on the CSV file
                WriteLine(writer => writer.WriteLine(ts.ToString(_timeStampFormat) + "," + sample.ToCsv()));
            }
        }
    }
}
