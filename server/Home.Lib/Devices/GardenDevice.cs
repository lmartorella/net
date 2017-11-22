using System.Linq;
using Lucky.Home.Sinks;
using System;
using System.Threading;

namespace Lucky.Home.Devices
{
    [Device("Garden")]
    [Requires(typeof(GardenSink))]
    public class GardenDevice : DeviceBase
    {
        private Timer _timer;

        public GardenDevice()
        {
            _timer = new Timer(o =>
            {
                if (IsFullOnline)
                {
                    Sinks.OfType<GardenSink>().First().Read();
                }
            }, null, 0, 500);
        }
    }
}
