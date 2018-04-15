using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lucky.Db
{
    public class TimeSample
    {
        private const string DAY_TS_FORMAT = "HH:mm:ss";

        [Csv(DAY_TS_FORMAT)]
        public DateTime TimeStamp;
    }

    public abstract class DayTimeSample<T> where T : TimeSample
    {
        private const string DAY_AGGR_FORMAT = "yyyy-MM-dd";

        [Csv(DAY_AGGR_FORMAT)]
        public DateTime Date;

        /// <summary>
        /// Aggregate one day of samples in a single sample line of a different time series
        /// </summary>
        public abstract bool Aggregate(DateTime date, IEnumerable<T> samples);
    }

    public interface ITimeSeries
    {
        /// <summary>
        /// Start
        /// </summary>
        Task Init(DateTime now);

        /// <summary>
        /// Change day
        /// </summary>
        void Rotate(DateTime start);
    }

    public interface ITimeSeries<T, Taggr> : ITimeSeries where T : TimeSample where Taggr : DayTimeSample<T>
    {
        /// <summary>
        /// Register new sample
        /// </summary>
        void AddNewSample(T sample);

        /// <summary>
        /// Get the current period data
        /// </summary>
        Taggr GetAggregatedData();
    }
}
