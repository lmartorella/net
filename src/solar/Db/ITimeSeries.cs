namespace Lucky.Home.Db;

/// <summary>
/// Csv for a day of samples, that supports custom aggregation
/// </summary>
public abstract class DayTimeSample<T> where T : TimeSample
{
    internal TimeSpan DaylightDelta = TimeSpan.Zero;

    /// <summary>
    /// Convert an invariant time to local time (DST/non-DST)
    /// </summary>
    public TimeSpan FromInvariantTime(TimeSpan ts)
    {
        return ts + DaylightDelta;
    }

    [Csv("yyyy-MM-dd")]
    public DateTime Date { get; set; }

    /// <summary>
    /// Aggregate one day of samples in a single sample line of a different time series
    /// </summary>
    public abstract bool Aggregate(DateTime date, IEnumerable<T> samples);
}

/// <summary>
/// A time series that support storage day-by-day
/// </summary>
public interface ITimeSeries
{
    /// <summary>
    /// Called at startup to sync aggregate data
    /// </summary>
    Task Init(DateTime now);

    /// <summary>
    /// Called at the end of a day to open a new csv file
    /// </summary>
    Task Rotate(DateTime start);
}
