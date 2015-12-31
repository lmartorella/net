using System;
using System.Linq;
using System.Threading;
using Lucky.Home.Serialization;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Passive/poll based switch array
    /// </summary>
    [SinkId("SWAR")]
    internal class DigitalInputArraySink : SinkBase, IDigitalInputArraySink
    {
        private TimeSpan _pollPeriod;
        private Timer _timer;
        private byte[] _lastData;
        private bool _isInitialized;
        private bool[] _status;

        public DigitalInputArraySink()
        {
            PollPeriod = TimeSpan.FromSeconds(1);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _isInitialized = true;
            // Start timer
            PollPeriod = PollPeriod;
            Status = new bool[0];
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ReadStatusResponse
        {
            public ushort SwitchCount;

            /// <summary>
            /// Switch data in bytes 
            /// </summary>
            [SerializeAsDynArray]
            public byte[] Data;
        }

        public bool[] Status
        {
            get { return _status; }
            private set
            {
                _status = value;
                if (StatusChanged != null)
                {
                    StatusChanged(this, EventArgs.Empty);
                }
            }
        }

        public TimeSpan PollPeriod
        {
            get { return _pollPeriod; }
            set
            {
                if (_isInitialized)
                {
                    if (_timer != null)
                    {
                        _timer.Dispose();
                    }
                    _pollPeriod = value;
                    _timer = new Timer(OnPoll, null, TimeSpan.Zero, _pollPeriod);
                }
            }
        }

        private void OnPoll(object state)
        {
            Read(reader =>
            {
                var resp = reader.Read<ReadStatusResponse>();
                if (_lastData != null && resp.Data.SequenceEqual(_lastData))
                {
                    // Same data. No event.
                }
                else
                {
                    // Something changed
                    _lastData = resp.Data;
                    int swCount = Math.Min(resp.SwitchCount, resp.Data.Length / 8);
                    var ret = new bool[swCount];
                    for (int i = 0; i < swCount; i++)
                    {
                        ret[i] = (_lastData[i / 8] & (1 << (i % 8))) != 0;
                    }
                    Status = ret;
                }
            });
        }

        public event EventHandler StatusChanged;
    }
}