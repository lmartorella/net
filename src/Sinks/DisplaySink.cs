using System;
using System.Threading;
using System.Text;
using Lucky.Home.Core;

namespace Lucky.Home.Sinks
{
    [DeviceId(DeviceIds.Display)]
    class DisplaySink : Sink
    {
        private Timer _timer;

        public DisplaySink()
        {
            _timer = new Timer(o => { SendHi(); }, null, 500, Timeout.Infinite);
        }

        private void SendHi()
        {
            Open();
            const string str = "Hello world.";
            Send(BitConverter.GetBytes((ushort)str.Length));
            Send(ASCIIEncoding.ASCII.GetBytes(str));
            Close();
        }
    }
}
