using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Lucky.Home;
using Lucky.Home.Core;

namespace Lucky.HomeMock.Core
{
    class HeloSender : Task
    {
        private readonly ushort _rcvPort;
        private readonly Timer _timer;
        private ILogger _logger;

        public HeloSender(ushort rcvPort)
        {
            _logger = Manager.GetService<ILoggerFactory>().Create("HeloSender");
            _rcvPort = rcvPort;
            // Install an auto-repeat timer until closed
            _timer = new Timer(HandleTick, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }

        public override void Dispose()
        {
            _timer.Dispose();
        }

        public bool Registered
        {
            get
            {
                return Manager.GetService<ControlPortListener>().State.DeviceId != Guid.Empty;
            }
        }

        private void HandleTick(object state)
        {
            // Send a broacast HELO message to port 17007
            MemoryStream stream = new MemoryStream();
            var msg = (Registered ? "HTBT" : "HEL3");
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write HOMEHELO
                writer.Write(Encoding.ASCII.GetBytes("HOME" + msg));
                writer.Write(Manager.GetService<ControlPortListener>().State.DeviceId.ToByteArray());
                writer.Write(BitConverter.GetBytes(_rcvPort));
            }
            byte[] dgram = stream.GetBuffer();
            UdpClient client = new UdpClient();
            client.Send(dgram, dgram.Length, new IPEndPoint(IPAddress.Broadcast, Constants.UdpControlPort));

            _logger.Log(msg + " sent");
        }
    }
}
