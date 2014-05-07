using System;
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
        private Timer _timer;

        public DisplaySink()
        {
            Schedule();
        }

        private void Schedule()
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
                    Message msg = new Message { Text = DateTime.Now.ToString("HH:mm:ss") };
                    NetSerializer<Message>.Write(msg, connection.Writer);
                    ErrorCode ack = (ErrorCode) connection.Reader.ReadUInt16();
                    if (ack != ErrorCode.Ok)
                    {
                        Logger.Log("Bad response  at " + this + ": " + ack);
                        return;
                    }
                }
                Schedule();
            }
            catch (Exception exc)
            {
                Logger.Log("SocketException " + exc.Message + " at " + this);
                _timer.Dispose();
            }
        }
    }
}
