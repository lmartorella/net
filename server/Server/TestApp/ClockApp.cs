using System;
using System.Threading;
using Lucky.Home.Application;
using Lucky.Home.Devices;

namespace Lucky.Home.TestApp
{
    class ClockApp : AppBase
    {
        private LineDevice _device;
        // ReSharper disable once NotAccessedField.Local
        private Timer _timer;

        protected internal override void OnInitialize()
        {
            base.OnInitialize();
            // Creates a line device
            
            _device = new LineDevice();
            _device.OnInitialize("", new SinkPath(new Guid("12345678-ABCD-EF00-1234-0123456789AB"), "LINE"));

            _timer = new Timer(state =>
            {
                _device.Write("Time: " + DateTime.Now.ToLongTimeString());
                _timer.Change(10000, Timeout.Infinite);
            }, null, 10000, Timeout.Infinite);
        }

        protected override void Dispose(bool dispose)
        {
            _device.Dispose();
            base.Dispose(dispose);
        }
    }
}
