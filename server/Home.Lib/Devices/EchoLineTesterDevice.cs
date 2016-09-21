using Lucky.Home.Sinks;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucky.Home.Devices
{
    [Device("Serial Echo")]
    [Requires(typeof(HalfDuplexLineSink))]
    public class EchoLineTesterDevice : DeviceBase
    {
        private Timer _timer;
        private string _lastMessage = "abcdefghijklmnop ";

        public EchoLineTesterDevice()
        {
            _timer = new Timer(o =>
            {
                if (IsFullOnline)
                {
                    Check();
                }
            }, null, 0, 2000);
        }

        private async void Check()
        {
            var ret = await Sinks.OfType<HalfDuplexLineSink>().First().SendReceive(Encoding.ASCII.GetBytes(_lastMessage));
            if (ret != null)
            {
                Console.WriteLine(Encoding.ASCII.GetString(ret));
                //_lastMessage = _lastMessage.ToUpper();
            }
        }
    }
}
