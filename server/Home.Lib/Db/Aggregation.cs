using System;

namespace Lucky.Home.Db
{
    class Aggregation<T>
    {
        /// <summary>
        /// Immediate value/average value
        /// </summary>
        T AverageValue { get; }

        /// <summary>
        /// Peak value
        /// </summary>
        T PeakValue { get; }

        /// <summary>
        /// Peak time
        /// </summary>
        DateTime PeakTime { get; }

        /// <summary>
        /// Sample period
        /// </summary>
        Tuple<DateTime, DateTime> Period;
    }
}
