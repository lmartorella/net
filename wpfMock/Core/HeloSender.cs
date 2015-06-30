using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Lucky.HomeMock.Core
{
    class HeloSender : Task
    {
        private readonly ushort _rcvPort;
        private readonly Timer _timer;
        private const int HeloProtocolPort = 17007;

        public HeloSender(ushort rcvPort)
        {
            _rcvPort = rcvPort;
            // Install an auto-repeat timer until closed
            _timer = new Timer(HandleTick, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }

        public override void Dispose()
        {
            _timer.Dispose();
        }

        public bool Registered { get; set; }

        private void HandleTick(object state)
        {
            // Send a broacast HELO message to port 17007
            MemoryStream stream = new MemoryStream();
            var msg = (Registered ? "HTBT" : "HEL3");
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write HOMEHELO
                writer.Write(Encoding.ASCII.GetBytes("HOME" + msg));
                writer.Write(Data.DeviceId.ToByteArray());
                writer.Write(BitConverter.GetBytes(_rcvPort));
            }
            byte[] dgram = stream.GetBuffer();
            UdpClient client = new UdpClient();
            client.Send(dgram, dgram.Length, new IPEndPoint(IPAddress.Broadcast, HeloProtocolPort));

            if (Sent != null)
            {
                Sent(this, new ItemEventArgs<string>(msg));
            }
        }

        public event EventHandler<ItemEventArgs<string>> Sent;
    }
}
