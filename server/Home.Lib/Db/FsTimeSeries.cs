using Lucky.Home.Notification;
using Lucky.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lucky.Home.Db
{
    /// <summary>
    /// Supports summer time translation
    /// </summary>
    class FsTimeSeries<T> : ITimeSeries<T> where T : ISupportCsv, new()
    {
        private string _folder;
        private FileInfo _fileName;

        private PeriodData _currentPeriod;
        private string _timeStampFormat;
        private string _header;
        private ILogger _logger;

        private class PeriodData
        {
            private List<Tuple<DateTime, T>> _data = new List<Tuple<DateTime, T>>();
            private readonly TimeSpan _daylightDelta = TimeSpan.Zero;

            public PeriodData(DateTime begin, bool useSummerTime)
            {
                Add(new T(), begin, true);

                if (begin.IsDaylightSavingTime() && useSummerTime)
                {
                    // Calc summer time offset
                    var rule = TimeZoneInfo.Local.GetAdjustmentRules().FirstOrDefault(r =>
                    {
                        return (begin > r.DateStart && begin < r.DateEnd);
                    });
                    if (rule != null)
                    {
                        _daylightDelta = rule.DaylightDelta;
                    }
                }
            }

            public void Add(T sample, DateTime ts)
            {
                Add(sample, ts, false);
            }

            private void Add(T sample, DateTime ts, bool init)
            {
                lock (_data)
                {
                    _data.Add(Tuple.Create(ts, sample));
                }
            }

            internal DateTime Adjust(DateTime ts)
            {
                return ts - _daylightDelta;
            }
        }

        public FsTimeSeries(string folderPath)
        {
            const string timeStampFormat = "HH:mm:ss";
            _logger = Manager.GetService<ILoggerFactory>().Create("Db/" + folderPath);
            _timeStampFormat = timeStampFormat;
            _folder = Manager.GetService<PersistenceService>().GetAppFolderPath("Db/" + folderPath);
            _fileName = new FileInfo("nul");
            _header = new T().CsvHeader;

            _logger.Log("Started");
        }

        private string ToPowerCvsName(DateTime now)
        {
            return now.ToString("yyyy-MM-dd") + ".csv";
        }

        public void Rotate(DateTime start)
        {
            var fileName = ToPowerCvsName(start);
            lock (_fileName)
            {
                var oldFileName = _fileName;

                // Change filename, so open a new file
                _fileName = new FileInfo(Path.Combine(_folder, fileName));
                _currentPeriod = new PeriodData(start, true);

                // Write CSV header
                WriteLine(writer => writer.WriteLine("TimeStamp," + _header));
                _logger.Log("Rotated", "date", start.Date);

                // Now the old file is free to be copied in backup
                CopyToBackup(oldFileName);
            }
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
                // Convert TS to non-daylight saving time
                ts = _currentPeriod.Adjust(ts);
                WriteLine(writer => writer.WriteLine(ts.ToString(_timeStampFormat) + "," + sample.ToCsv()));
            }
        }

        private void CopyToBackup(FileInfo oldFileName)
        {
            if (oldFileName.Exists)
            {
                var targetFile = GetBackupPath(oldFileName);
                try
                {
                    oldFileName.CopyTo(targetFile.FullName);
                }
                catch (Exception exc)
                {
                    _logger.Exception(exc);
                    Manager.GetService<INotificationService>().EnqueueStatusUpdate("File locked", "Cannot backup the db file: " + oldFileName.FullName + Environment.NewLine + "EXC: " + exc.Message);
                }
            }
        }

        private FileInfo GetBackupPath(FileInfo srcFileName)
        {
            DirectoryInfo targetDir = new DirectoryInfo(Path.Combine(srcFileName.Directory.FullName, "backup"));
            if (!targetDir.Exists)
            {
                targetDir.Create();
            }
            string baseName = srcFileName.Name;
            string targetFileName = baseName;
            var targetFile = new FileInfo(Path.Combine(targetDir.FullName, targetFileName));
            int c = 1;
            while (targetFile.Exists)
            {
                targetFileName = baseName + string.Format(" ({0})", c++);
                targetFile = new FileInfo(Path.Combine(targetDir.FullName, targetFileName));
            }
            return targetFile;
        }
    }
}
