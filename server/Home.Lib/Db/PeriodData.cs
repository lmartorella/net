using System;
using System.Collections.Generic;

namespace Lucky.Home.Db
{
    interface ISupportAverage<T> : IComparable<T>
    {
        T Add(T t1);
        T Mul(double d);
        T Div(double d);
    }

    class PeriodData<T> where T : ISupportAverage<T>
    {
        private List<Tuple<DateTime, T>> _data = new List<Tuple<DateTime, T>>();
        private T Sum = default(T);
        private DateTime _begin;
        private DateTime _lastTs;
        private T _minV;
        private T _maxV;
        private DateTime _minT;
        private DateTime _maxT;

        public PeriodData(DateTime begin)
        {
            _begin = _lastTs = begin;
            Add(default(T), begin, true);
        }

        public void Add(T sample, DateTime ts)
        {
            Add(sample, ts);
        }

        private void Add(T sample, DateTime ts, bool init)
        {
            lock (_data)
            {
                _data.Add(Tuple.Create(ts, sample));
                var weight = (ts - _lastTs).TotalSeconds;
                Sum = Sum.Add(sample.Mul(weight));

                LastSample = sample;
                _lastTs = ts;

                if (init || sample.CompareTo(_minV) < 0)
                {
                    _minV = sample;
                    _minT = ts;
                }
                if (init || sample.CompareTo(_maxV) > 0)
                {
                    _maxV = sample;
                    _maxT = ts;
                }
            }
        }

        public T LastSample { get; private set; }

        public DateTime Begin
        {
            get
            {
                return _begin;
            }
        }

        public Aggregation<T> GetAggregation(DateTime ts)
        {
            lock (_data)
            {
                var totalW = (ts - _begin).TotalSeconds;
                var avg = Sum.Div(totalW);
                return new Aggregation<T>
                {
                    AverageValue = avg,
                    Period = Tuple.Create(_begin, ts),
                    MinTime = _minT,
                    MaxTime = _maxT,
                    MinValue = _minV,
                    MaxValue = _maxV
                };
            }
        }
    }
}
