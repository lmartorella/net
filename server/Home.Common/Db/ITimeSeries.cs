using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lucky.Db
{
    public class TimeSample
    {
        [Csv("HH:mm:ss")]
        public DateTime TimeStamp;
    }

    public abstract class DayTimeSample<T> where T : TimeSample
    {
        [Csv("yyyy-MM-dd")]
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
