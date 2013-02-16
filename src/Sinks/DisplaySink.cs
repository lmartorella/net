using System;
using System.Threading;
using System.Text;
using Lucky.Home.Core;

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

        private void SendHi()
        {
            using (var connection = Open())
            {
                const string str = "Hello world.";
                connection.Writer.Write(BitConverter.GetBytes((ushort)str.Length));
                connection.Writer.Write(ASCIIEncoding.ASCII.GetBytes(str));
            }
        }
    }
}
