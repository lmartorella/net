using System.Linq;
using Lucky.Home.Sinks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Lucky.Home.Devices
{
    [Device("Clock")]
    [RequiresArray(typeof(DisplaySink))]
    public class ClockDevice : DeviceBase
    {
        private Timer _timer;

        public ClockDevice()
        {
            _timer = new Timer(async o =>
            {
                if (IsFullOnline)
                {
                    var str = DateTime.Now.ToString("HH:mm:ss");
                    await Write(str);
                }
            }, null, 0, 500);
        }

        private async Task Write(string str)
        {
            foreach (var sink in Sinks.OfType<DisplaySink>())
            {
                await sink.Write(str);
            }
        }
    }
}
