using System;
using System.Collections.Generic;

namespace Lucky.Home.Db
{
    interface ISample<T, out Taggr> where Taggr : class
    {
        /// <summary>
        /// Return a single line
        /// </summary>
        /// <returns></returns>
        string ToCsv();

        /// <summary>
        /// Get the header
        /// </summary>
        string CsvHeader { get; }

        /// <summary>
        /// Aggregate one day of samples in a single sample line of a different time series
        /// </summary>
        Taggr Aggregate(IEnumerable<Tuple<T, DateTime>> samples);
    }

    interface ITimeSeries
    {
        /// <summary>
        /// Change day
        /// </summary>
        void Rotate(DateTime start);
    }

    interface ITimeSeries<T, Taggr> : ITimeSeries where T : ISample<T, Taggr> where Taggr : class
    {
        /// <summary>
        /// Register new sample
        /// </summary>
        void AddNewSample(T sample, DateTime ts);
    }
}
