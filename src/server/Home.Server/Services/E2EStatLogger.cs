using Lucky.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucky.Home.Services
{
    class E2EStatLogger
    {
        private NodeId _id;
        private List<TimeSpan> _lastWindow = new List<TimeSpan>();

        public E2EStatLogger(NodeId id)
        {
            _id = id;
            var service = Manager.GetService<E2EStatService>();
            service.Tick += (o, e) =>
            {
                // Calc avg, min and max
                double min = -1, max = -1, average = -1;
                lock (_lastWindow)
                {
                    if (_lastWindow.Count > 0)
                    {
                        min = _lastWindow.Min(t => t.TotalMilliseconds);
                        max = _lastWindow.Max(t => t.TotalMilliseconds);
                        average = _lastWindow.Average(t => t.TotalMilliseconds);
                        _lastWindow.Clear();
                    }
                }
                if (max > 0)
                {
                    service.Report(DateTime.Now, id.ToString(), min, max, average);
                }
            };
        }

        public void AddE2EReadSample(TimeSpan readTime)
        {
            lock (_lastWindow)
            {
                _lastWindow.Add(readTime);
            }
        }
    }
}
