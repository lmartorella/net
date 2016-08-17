using System;

namespace Lucky.Home.Db
{
    class Aggregation<T>
    {
        /// <summary>
        /// Immediate value/average value
        /// </summary>
        public T AverageValue;

        /// <summary>
        /// Peak value
        /// </summary>
        public T MaxValue;

        /// <summary>
        /// Peak time
        /// </summary>
        public DateTime MaxTime;

        /// <summary>
        /// Peak value
        /// </summary>
        public T MinValue;

        /// <summary>
        /// Peak time
        /// </summary>
        public DateTime MinTime;

        /// <summary>
        /// Sample period
        /// </summary>
        public Tuple<DateTime, DateTime> Period;
    }
}
