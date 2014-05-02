using System.Net.Sockets;
using System.Threading;
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
        private readonly Timer _timer;

        public DisplaySink()
        {
            _timer = new Timer(SendHi, null, 500, Timeout.Infinite);
        }

        private class Message
        {
            [SerializeAsDynArray]
            public string Text;
        }

        private void SendHi(object o)
        {
            try
            {
                using (var connection = Open())
                {
                    Message msg = new Message { Text = "Hello world." };
                    NetSerializer<Message>.Write(msg, connection.Writer);
                }
            }
            catch (SocketException)
            {
                _timer.Dispose();
            }
        }
    }
}
