using Lucky.Db;
using Lucky.Home.Notification;
using Lucky.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lucky.Home.Db
{
    /// <summary>
    /// Supports summer time translation
    /// </summary>
    public class FsTimeSeries<T, Taggr> : ITimeSeries<T, Taggr> where T : TimeSample, new() where Taggr : DayTimeSample<T>, new()
    {
        private const string FILENAME_FORMAT = "yyyy-MM-dd";

        private DirectoryInfo _folder;
        private FileInfo _dayFile;
        private FileInfo _aggrFile;

        private PeriodData _currentPeriod;
        private ILogger _logger;
        private readonly object _lockDb = new object();

        private class PeriodData
        {
            private List<T> _data = new List<T>();
            private readonly TimeSpan _daylightDelta = TimeSpan.Zero;

            public DateTime Begin { get; }

            public PeriodData(DateTime begin, bool useSummerTime)
            {
                Begin = begin;
                Enqueue(new T() { TimeStamp = begin }, true);

                // Check if DST. However DST is usually starting at 3:00 AM, so use midday time
                if ((begin.Date + TimeSpan.FromHours(12)).IsDaylightSavingTime() && useSummerTime)
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

            public void ParseCsv(FileInfo file)
            {
                foreach (var sample in CsvHelper<T>.ReadCsv(file))
                {
                    Add(sample);
                }
            }

            public void Add(T sample)
            {
                Enqueue(sample, false);
            }

            private void Enqueue(T sample, bool init)
            {
                lock (_data)
                {
                    _data.Add(sample);
                }
            }

            internal DateTime Adjust(DateTime ts)
            {
                return ts - _daylightDelta;
            }

            public Taggr GetAggregatedData()
            {
                var ret = new Taggr();
                lock (_data)
                {
                    if (ret.Aggregate(Begin.Date, _data))
                    {
                        return ret;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        public FsTimeSeries(string folderPath)
        {
            _logger = Manager.GetService<ILoggerFactory>().Create("Db/" + folderPath);
            _folder =  new DirectoryInfo(Manager.GetService<PersistenceService>().GetAppFolderPath("Db/" + folderPath));
            _aggrFile = new FileInfo(Path.Combine(_folder.FullName, "_aggr.csv"));

            _logger.Log("Started");
        }

        private static string CalcDayCvsName(DateTime now)
        {
            return now.ToString(FILENAME_FORMAT) + ".csv";
        }

        private static DateTime? ParseDate(string name)
        {
            DateTime date;
            if (DateTime.TryParseExact(Path.GetFileNameWithoutExtension(name), FILENAME_FORMAT, null, System.Globalization.DateTimeStyles.None, out date))
            {
                return date;
            }
            else
            {
                return null;
            }
        }

        public Task Init(DateTime start)
        {
            return Task.Run(() =>
            {
                SetupCurrentDb(start, true);
                AggregateData(_logger, _folder, start);
            });
        }

        public void Rotate(DateTime start)
        {
            SetupCurrentDb(start, false);
            _logger.Log("Rotated", "date", start.Date);
        }

        private void SetupCurrentDb(DateTime start, bool init)
        { 
            var fileName = CalcDayCvsName(start);
            lock (_lockDb)
            {
                var oldFileName = _dayFile;
                var oldPeriod = _currentPeriod;

                // Change filename, so open a new file
                _dayFile = new FileInfo(Path.Combine(_folder.FullName, fileName));
                _currentPeriod = new PeriodData(start, true);

                // Write CSV header
                if (!_dayFile.Exists)
                {
                    CsvHelper<T>.WriteCsvHeader(_dayFile);
                }
                else
                {
                    // Read the CSV and populate the current Period Data
                    _currentPeriod.ParseCsv(_dayFile);
                }

                // Now the old file is free to be copied in backup
                if (!init)
                {
                    CopyToBackup(oldFileName);

                    // Generate aggregated data and log it in a separate DB
                    var aggrData = oldPeriod.GetAggregatedData();
                    if (aggrData != null)
                    {
                        aggrData.Date = oldPeriod.Begin;

                        if (!_aggrFile.Exists)
                        {
                            CsvHelper<Taggr>.WriteCsvHeader(_aggrFile);
                        }
                        CsvHelper<Taggr>.WriteCsvLine(_aggrFile, aggrData);
                    }
                }
            }
        }

        public Taggr GetAggregatedData()
        {
            return _currentPeriod.GetAggregatedData();
        }

        public void AddNewSample(T sample)
        {
            lock (_lockDb)
            {
                // Convert TS to non-daylight saving time
                sample.TimeStamp = _currentPeriod.Adjust(sample.TimeStamp);

                // Add it to the aggregator
                _currentPeriod.Add(sample);

                // In addition, write immediately on the CSV file (for the web server)\
                CsvHelper<T>.WriteCsvLine(_dayFile, sample);
            }
        }

        private void CopyToBackup(FileInfo oldFileName)
        {
            if (oldFileName.Exists)
            {
                var targetFile = GetBackupPath(oldFileName);
                try
                {
                    _logger.Log("Making backup", "src", oldFileName.FullName, "dst", targetFile.FullName);
                    oldFileName.CopyTo(targetFile.FullName);
                }
                catch (Exception exc)
                {
                    _logger.Exception(exc);
                    Manager.GetService<INotificationService>().EnqueueStatusUpdate("File locked", "Cannot backup the db file: " + oldFileName.FullName + Environment.NewLine + "EXC: " + exc.Message);
                }
            }
            else
            {
                _logger.Log("Cannot find file to backup", "src", oldFileName.FullName);
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

        public static void AggregateData(ILogger logger, DirectoryInfo folder, DateTime now)
        {
            FileInfo aggrFile = new FileInfo(Path.Combine(folder.FullName, "_aggr.csv"));
            if (aggrFile.Exists)
            {
                logger.Log("Aggregated csv already exists, using it");
                return;
            }

            CsvHelper<Taggr>.WriteCsvHeader(aggrFile);

            // Skip the current/future dates
            var files = folder.GetFiles().Select(file => Tuple.Create(file, ParseDate(file.Name))).Where(t => t.Item2.HasValue && t.Item2.Value < now.Date).OrderBy(t => t.Item2).ToArray();
            logger.Log("Parsing csv files", "n", files.Length);
            foreach (var tuple in files)
            {
                // Parse CSV
                var aggrData = CsvAggregate<T, Taggr>.ParseCsv(tuple.Item1, tuple.Item2.Value);
                if (aggrData != null)
                {
                    CsvHelper<Taggr>.WriteCsvLine(aggrFile, aggrData);
                }
            }
            logger.Log("Parsing csv files done");
        }
    }
}
