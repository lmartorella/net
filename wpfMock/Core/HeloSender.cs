using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Lucky.Home;
using Lucky.Services;

namespace Lucky.HomeMock.Core
{
    class HeloSender : Task
    {
        private readonly ushort _rcvPort;
        private readonly bool _localhostMode;
        private readonly Timer _timer;
        private readonly ILogger _logger;

        public HeloSender(ushort rcvPort, bool localhostMode)
        {
            _logger = Manager.GetService<ILoggerFactory>().Create("HeloSender");
            _rcvPort = rcvPort;
            _localhostMode = localhostMode;
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
            var ipAddress = _localhostMode ? new IPAddress(0x0100007f) : IPAddress.Broadcast;
            client.Send(dgram, dgram.Length, new IPEndPoint(ipAddress, Constants.UdpControlPort));

            _logger.Log(msg + " sent");
        }
    }
}
