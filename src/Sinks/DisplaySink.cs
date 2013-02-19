using System;
using System.Linq;
using System.Threading;
using System.Text;
using Lucky.Home.Core;
using Lucky.Home.Core.Serialization;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Simple display protocol
    /// </summary>
    /// <remarks>
    /// Protocol: TCP_OPEN, 
    ///          -> [word:lenght] + ASCII
    ///           TCP_CLOSE
    /// </remarks>
    [DeviceId(DeviceIds.Display)]
    class DisplaySink : Sink
    {
        private Timer _timer;

        public DisplaySink()
        {
            _timer = new Timer(o => { SendHi(); }, null, 500, Timeout.Infinite);
        }

        private class Message
        {
            [SerializeAsDynArray]
            public string Text;
        }

        private void SendHi()
        {
            using (var connection = Open())
            {
                Message msg = new Message { Text = "Hello world." };
                NetSerializer<Message>.Write(msg, connection.Writer);
            }
        }
    }
}
