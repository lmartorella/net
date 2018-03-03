using System;

namespace Lucky.Home.Db
{
    interface ITimeSeries<T>
    {
        /// <summary>
        /// Register new sample
        /// </summary>
        void AddNewSample(T sample, DateTime ts);
    }
}
