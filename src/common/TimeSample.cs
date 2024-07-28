using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Lucky.Home;

/// <summary>
/// Csv basic type of a time-stamp based record
/// </summary>
public class TimeSample
{
    [IgnoreDataMember]
    [JsonIgnore]
    public TimeSpan DaylightDelta = TimeSpan.Zero;

    /// <summary>
    /// Convert an invariant time to local time (DST/non-DST)
    /// </summary>
    public TimeSpan FromInvariantTime(TimeSpan ts)
    {
        return ts + DaylightDelta;
    }

    /// <summary>
    /// Convert an invariant time to local time (DST/non-DST)
    /// </summary>
    public DateTime FromInvariantTime(DateTime dt)
    {
        return dt + DaylightDelta;
    }

    [Csv("HH:mm:ss")]
    public DateTime TimeStamp { get; set; }
}