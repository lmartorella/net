using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Home.Db;

/// <summary>
/// A day-by-day time series that stores data in CSV files (one for each day).
/// Supports DST translation of timestamp (stores everything in non-DST time) for easy comparison (e.g. solar panel outputs).
/// Stores an additional aggregation CSV (_aggr.csv) for quick startup after restart.
/// </summary>
public class FsTimeSeries<T, Taggr> : BackgroundService where T : TimeSample, new() where Taggr : DayTimeSample<T>, new()
{
    public const string FILENAME_FORMAT = "yyyy-MM-dd";

    private DirectoryInfo _folder;
    private FileInfo _dayFile;
    private FileInfo _aggrFile;

    private PeriodData _currentPeriod;
    private ILogger<FsTimeSeries<T, Taggr>> logger;
    private readonly NotificationService notificationService;
    private readonly object _lockDb = new object();

    private class PeriodData
    {
        private List<T> _data = new List<T>();
        private readonly TimeSpan _daylightDelta = TimeSpan.Zero;

        public DateTime Begin { get; }

        public PeriodData(DateTime begin, bool useSummerTime)
        {
            Begin = begin;
            Add(new T() { TimeStamp = begin }, true);

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
                Add(sample, false);
            }
        }

        internal T GetLastSample()
        {
            lock (_data)
            {
                // Skip the first, added in the ctor to maintain the begin timestamp
                return _data.Skip(1).LastOrDefault();
            }
        }

        public void Add(T sample, bool convert)
        {
            lock (_data)
            {
                // Convert TS to non-daylight saving time
                if (convert)
                {
                    sample.TimeStamp = ToInvariantTime(sample.TimeStamp);
                }
                sample.DaylightDelta = _daylightDelta;
                _data.Add(sample);
            }
        }

        /// <summary>
        /// Convert a DST/non-DST time to invariant non-DST time
        /// </summary>
        private DateTime ToInvariantTime(DateTime ts)
        {
            return ts - _daylightDelta;
        }

        public Taggr GetAggregatedData()
        {
            var ret = new Taggr();
            ret.DaylightDelta = _daylightDelta;
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

    /// <summary>
    /// Build a time series manager in a folder.
    /// </summary>
    /// <param name="folderPath">The relative path to the {etc}/Db/ path.</param>
    public FsTimeSeries(ILogger<FsTimeSeries<T, Taggr>> logger, NotificationService notificationService)
    {
        this.logger = logger;
        this.notificationService = notificationService;

        _folder =  new DirectoryInfo(Path.Join("Db", "SOLAR"));
        _aggrFile = new FileInfo(Path.Combine(_folder.FullName, "_aggr.csv"));

        this.logger.LogInformation("Started");
    }

    private static string CalcDayCvsName(DateTime now)
    {
        return now.ToString(FILENAME_FORMAT) + ".csv";
    }

    private DateTime _nextPeriodStart;
    private readonly TimeSpan PeriodLength = TimeSpan.FromDays(1);
    private Timer _timerMinute;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var start = DateTime.Now;
        await SetupCurrentDb(start, true);
        AggregateData(logger, _folder, start);

        // Rotate solar db at midnight 
        var periodStart = _nextPeriodStart = DateTime.Now.Date;
        while (DateTime.Now >= _nextPeriodStart)
        {
            _nextPeriodStart += PeriodLength;
        }
        _timerMinute = new Timer(s =>
        {
            if (DateTime.Now >= _nextPeriodStart)
            {
                while (DateTime.Now >= _nextPeriodStart)
                {
                    _nextPeriodStart += PeriodLength;
                }
                Rotate(DateTime.Now);
            }
        }, null, 0, 30 * 1000);

    }

    /// <summary>
    /// Called at the end of a day to open a new csv file
    /// </summary>
    private async Task Rotate(DateTime start)
    {
        await SetupCurrentDb(start, false);
        logger.LogInformation("Rotated: date {0}", start.Date);
    }

    private async Task SetupCurrentDb(DateTime start, bool init)
    { 
        var fileName = CalcDayCvsName(start);
        Task copy = null;
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
                copy = CopyToBackup(oldFileName);

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

        if (copy != null)
        {
            await copy;
        }
    }

    /// <summary>
    /// Return aggregated data for the whole DB.
    /// </summary>
    public Taggr GetAggregatedData()
    {
        return _currentPeriod.GetAggregatedData();
    }

    /// <summary>
    /// Register new sample
    /// </summary>
    public void AddNewSample(T sample)
    {
        lock (_lockDb)
        {
            // Add it to the aggregator first, so daylight time will be updated
            _currentPeriod.Add(sample, true);

            // In addition, write immediately on the CSV file.
            CsvHelper<T>.WriteCsvLine(_dayFile, sample);
        }
    }

    public T GetLastSample()
    {
        lock (_lockDb)
        {
            return _currentPeriod.GetLastSample();
        }
    }

    private async Task CopyToBackup(FileInfo oldFileName)
    {
        int retry = 0;
        oldFileName.Refresh();
        while (!oldFileName.Exists)
        {
            await Task.Delay(2000);
            if (retry++ == 5)
            {
                notificationService.EnqueueStatusUpdate("Errori DB", "Cannot find file to backup after 5 retries");
                logger.LogInformation("Cannot find file to backup after 5 retries", "src", oldFileName.FullName);
                return;
            }
            oldFileName.Refresh();
        }

        var targetFile = GetBackupPath(oldFileName);
        try
        {
            logger.LogInformation("Making backup: src {0} dst {1}", oldFileName.FullName, targetFile.FullName);
            oldFileName.CopyTo(targetFile.FullName);
        }
        catch (Exception exc)
        {
            logger.LogError(exc, "locking {0}", oldFileName.FullName);
            notificationService.EnqueueStatusUpdate("File locked", "Cannot backup the db file: " + oldFileName.FullName + Environment.NewLine + "EXC: " + exc.Message);
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

    /// <summary>
    /// Aggregate whole DB content at startup
    /// </summary>
    public static void AggregateData(ILogger logger, DirectoryInfo directory, DateTime now)
    {
        FileInfo aggrFile = new FileInfo(Path.Combine(directory.FullName, "_aggr.csv"));
        if (aggrFile.Exists)
        {
            logger.LogInformation("Aggregated csv already exists, using it");
            return;
        }

        CsvHelper<Taggr>.WriteCsvHeader(aggrFile);

        // Skip the current/future dates
        var files = CsvAggregate<T, Taggr>.GetFilesInFolder(directory, FILENAME_FORMAT, now);
        logger.LogInformation("Parsing csv files: n {0}", files.Length);
        foreach (var tuple in files)
        {
            // Parse CSV
            var aggrData = CsvAggregate<T, Taggr>.ParseCsv(tuple.Item1, tuple.Item2);
            if (aggrData != null)
            {
                CsvHelper<Taggr>.WriteCsvLine(aggrFile, aggrData);
            }
        }
        logger.LogInformation("Parsing csv files done");
    }
}
