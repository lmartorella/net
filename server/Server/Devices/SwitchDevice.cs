using System;
using Lucky.Home.Sinks;

namespace Lucky.Home.Devices
{
    internal class SwitchDevice : DeviceBase<ISwitchArraySink>, ISwitchDevice
    {
        private readonly int _bit;
        private ISwitchArraySink _sink;
        private bool _lastStatus;

        public SwitchDevice(int bit)
        {
            _bit = bit;
        }

        protected override void OnSinkChanged()
        {
            base.OnSinkChanged();
            if (_sink != null)
            {
                _sink.StatusChanged -= HandleStatusChanged;
            }
            _sink = Sink;
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