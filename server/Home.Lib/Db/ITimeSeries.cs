using System;

namespace Lucky.Home.Db
{
    interface ITimeSeries<T>
    {
        /// <summary>
        /// Current values
        /// </summary>
        T ImmediateData { get; }

        /// <summary>
        /// Current period, from the last rotation
        /// </summary>
        Aggregation<T> CurrentPeriodData { get; }

        /// <summary>
        /// Previous period, before the previous rotation
        /// </summary>
        Aggregation<T> LastPeriodData { get; }

        /// <summary>
        /// From a custom period
        /// </summary>
        Aggregation<T> FromCustomPeriod(DateTime start, DateTime end);
    }
}
