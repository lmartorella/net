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
                    var sink = Sinks.OfType<GardenSink>().First();
                    bool isAval = sink.Read();
                    if (isAval)
                    {
                        sink.WriteProgram(new int[] { 0, 0, 0, 1 });
                    }
                }
            }, null, 0, 1000);
        }
    }
}
