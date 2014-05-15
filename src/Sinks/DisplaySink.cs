using System;
using System.Threading;
using Lucky.Home.Core;
using Lucky.Home.Core.Serialization;

// ReSharper disable once UnusedMember.Global

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
    [DeviceId(DisplaySinkid)]
    class DisplaySink : Sink
    {
        private Timer _timer;
        private const int DisplaySinkid = 1;

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
                    connection.Write(msg);
                    ErrorCode ack = connection.Read<ErrorCode>();
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
