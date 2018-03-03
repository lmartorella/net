using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucky.Home.Db
{
    class PeriodData<T> where T : IComparable<T>, new()
    {
        private List<Tuple<DateTime, T>> _data = new List<Tuple<DateTime, T>>();
        private readonly TimeSpan _daylightDelta = TimeSpan.Zero;

        public PeriodData(DateTime begin, bool useSummerTime)
        {
            Add(new T(), begin, true);

            if (begin.IsDaylightSavingTime() && useSummerTime)
            {
                // Calc summer time offset
                var rule = TimeZoneInfo.Local.GetAdjustmentRules().FirstOrDefault(r =>
                {
                    return (begin > r.DateStart && begin < r.DateEnd);
                });
                if (rule != null)
                {
                    _daylightDelta = rule.DaylightDelta;
                }
            }
        }

        public void Add(T sample, DateTime ts)
        {
            Add(sample, ts, false);
        }

        private void Add(T sample, DateTime ts, bool init)
        {
            lock (_data)
            {
                _data.Add(Tuple.Create(ts, sample));
            }
        }

        internal DateTime Adjust(DateTime ts)
        {
            return ts - _daylightDelta;
        }
    }
}
