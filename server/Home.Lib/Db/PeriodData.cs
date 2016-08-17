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
        private bool _hasMax;
        private bool _hasMin;

        public PeriodData(DateTime begin)
        {
            _begin = _lastTs = begin;
            Add(default(T), begin);
        }

        private void Add(T sample, DateTime ts)
        {
            _data.Add(Tuple.Create(ts, sample));
            var weight = (ts - _lastTs).TotalSeconds;
            Sum = Sum.Add(sample.Mul(weight));

            LastSample = sample;
            _lastTs = ts;

            if (!_hasMin || sample.CompareTo(_minV) < 0)
            {
                _minV = sample;
                _minT = ts;
                _hasMin = true;
            }
            if (!_hasMax || sample.CompareTo(_maxV) > 0)
            {
                _maxV = sample;
                _maxT = ts;
                _hasMax = true;
            }
        }

        public T LastSample { get; private set; }

        public Aggregation<T> Average
        {
            get
            {
                var ts = DateTime.Now;
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
