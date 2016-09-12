using System.Linq;
using Lucky.Home.Sinks;
using System;
using System.Threading;

namespace Lucky.Home.Devices
{
    [Device("Clock")]
    [RequiresArray(typeof(DisplaySink))]
    public class ClockDevice : DeviceBase
    {
        private Timer _timer;

        public ClockDevice()
        {
            _timer = new Timer(o =>
            {
                if (IsFullOnline)
                {
                    var str = DateTime.Now.ToString("HH:mm:ss");
                    Write(str);
                }
            }, null, 0, 500);
        }

        private void Write(string str)
        {
            foreach (var sink in Sinks.OfType<DisplaySink>())
            {
                sink.Write(str);
            }
        }
    }
}
