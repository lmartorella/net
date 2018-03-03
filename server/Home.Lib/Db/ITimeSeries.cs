using System;

namespace Lucky.Home.Db
{
    interface ITimeSeries
    {
        /// <summary>
        /// Change day
        /// </summary>
        void Rotate(DateTime start);
    }

    interface ITimeSeries<T> : ITimeSeries
    {
        /// <summary>
        /// Register new sample
        /// </summary>
        void AddNewSample(T sample, DateTime ts);
    }
}
