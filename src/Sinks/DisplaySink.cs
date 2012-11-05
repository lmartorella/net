using System;
using System.Threading;
using Lucky.Home.Core;

namespace Lucky.Home.Sinks
{
    [DeviceId(1)]
    class DisplaySink : Sink
    {
        private Timer _timer;

        public DisplaySink()
        {
            _timer = new Timer(o => { SendHi(); });
        }

        private void SendHi()
        {

        }
    }
}
