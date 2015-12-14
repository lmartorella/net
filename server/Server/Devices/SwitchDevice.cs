using System;
using Lucky.Home.Sinks;

// ReSharper disable once UnusedMember.Global

namespace Lucky.Home.Devices
{
    internal class SwitchDevice : DeviceBase<ISwitchArraySink>, ISwitchDevice
    {
        private int _bit;
        private ISwitchArraySink _sink;
        private bool _lastStatus;
        private TimeSpan _period;

        /// <summary>
        /// argument = "bit#,poll_ms#"
        /// </summary>
        public override void OnInitialize(string argument, SinkPath sinkPath)
        {
            var args = argument.Split(',');
            _bit = int.Parse(args[0]);
            _period = TimeSpan.FromMilliseconds(double.Parse(args[1]));
            base.OnInitialize(argument, sinkPath);
        }

        protected override void OnSinkChanged()
        {
            base.OnSinkChanged();
            if (_sink != null)
            {
                _sink.StatusChanged -= HandleStatusChanged;
            }
            _sink = Sink;
            _sink.PollPeriod = _period;
            if (_sink != null)
            {
                _sink.StatusChanged += HandleStatusChanged;
            }
        }

        private void HandleStatusChanged(object sender, EventArgs e)
        {
            Status = _bit < Sink.Status.Length && Sink.Status[_bit];
        }

        protected override void Dispose(bool disposing)
        {
            if (_sink != null)
            {
                _sink.StatusChanged -= HandleStatusChanged;
            }
            base.Dispose(disposing);
        }

        public bool Status
        {
            get 
            {
                return _lastStatus;
            }
            private set
            {
                if (_lastStatus != value)
                {
                    _lastStatus = value;
                    if (StatusChanged != null)
                    {
                        StatusChanged(this, EventArgs.Empty);
                    }
                }
            }
        }

        public event EventHandler StatusChanged;
    }
}