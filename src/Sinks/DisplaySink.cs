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
            _timer = new Timer(o => { SendHi(); });
        }

        private void SendHi()
        {
            Open();
            const string str = "Hello world.";
            Send(BitConverter.GetBytes((short)str.Length));
            Send(ASCIIEncoding.ASCII.GetBytes(str));
            Close();
        }
    }
}
