using Lucky.Services;
using System;

namespace Lucky.Home.Db
{
    class FsTimeSeries<T> : ITimeSeries<T>
    {
        private string _folder;
        private string _fileName;

        private PeriodData<T> _currentPeriod = new PeriodData<T>();
        private PeriodData<T> _lastPeriod = new PeriodData<T>();

        public FsTimeSeries(string folderPath)
        {
            _folder = Manager.GetService<PersistenceService>().GetAppFolderPath("Db/" + folderPath);
        }

        internal void Rotate(string fileName)
        {
            _fileName = fileName;
            _lastPeriod = _currentPeriod;
            _currentPeriod = new PeriodData<T>();
        }

        public T ImmediateData
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
                return _currentPeriod.Aggregation;
            }
        }

        public Aggregation<T> LastPeriodData
        {
            get
            {
                return _lastPeriod.Aggregation;
            }
        }

        public Aggregation<T> FromCustomPeriod(DateTime start, DateTime end)
        {
            throw new NotImplementedException();
        }
    }
}
