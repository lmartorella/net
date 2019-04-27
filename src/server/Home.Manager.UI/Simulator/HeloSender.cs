using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Lucky.Home.Services;

namespace Lucky.Home.Simulator
{
    /// <summary>
    /// UDP packet sender
    /// </summary>
    class HeloSender : IDisposable
    {
        private readonly ushort _rcvPort;
        private readonly bool _localhostMode;
        private readonly Timer _timer;
        private readonly ILogger _logger;
        private readonly ISimulatedNode _node;
        public bool ChildChanged = false;

        public HeloSender(ushort rcvPort, bool localhostMode, ISimulatedNode node)
        {
            _logger = node.Logger;
            _node = node;
            _rcvPort = rcvPort;
            _localhostMode = localhostMode;
            // Install an auto-repeat timer until closed
            _timer = new Timer(HandleTick, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public bool Registered
        {
            get
            {
                return _node.StateProvider.Id != Guid.Empty;
            }
        }

        private void HandleTick(object state)
        {
            // Send a broacast HELO message to port 17007
            MemoryStream stream = new MemoryStream();
            var msg = "HEL4";
            if (Registered)
            {
                msg = ChildChanged ? "CCHN" : "HTBT";
            }
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write HOMEHELO
                writer.Write(Encoding.ASCII.GetBytes("HOME" + msg));
                writer.Write(_node.StateProvider.Id.ToByteArray());
                writer.Write(BitConverter.GetBytes(_rcvPort));
                // If child changed, add a mask for changes
                if (ChildChanged)
                {
                    // Write num of bytes (1)
                    writer.Write(BitConverter.GetBytes((short)1));
                    // Child changed: 1
                    writer.Write((byte)1);
                }
            }
            byte[] dgram = stream.ToArray();
            UdpClient client = new UdpClient();
            var ipAddress = _localhostMode ? new IPAddress(0x0100007f) : IPAddress.Broadcast;

            int port =
#if DEBUG
                Constants.UdpControlPort_Debug;
#else
                Constants.UdpControlPort;
#endif
            client.Send(dgram, dgram.Length, new IPEndPoint(ipAddress, port));

            _logger.Log(msg + " sent");
        }
    }
}
