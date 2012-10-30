using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Lucky.HomeMock.Core
{
    class HeloSender : IDisposable
    {
        private Timer _timer;

        public HeloSender()
        {
            // Install an auto-repeat timer until closed
            _timer = new Timer(HandleTick, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        private void HandleTick(object state)
        {
            // Send a broacast HELO message to port 17007
            MemoryStream stream = new MemoryStream();
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write HOMEHELO
                writer.Write(ASCIIEncoding.ASCII.GetBytes("HOMEHELO0"));
                writer.Write(Data.DeviceId.ToByteArray());

            }
            byte[] dgram = stream.GetBuffer();
            UdpClient client = new UdpClient();
            client.Send(dgram, dgram.Length, new IPEndPoint(IPAddress.Broadcast, 17007));

            if (Sent != null)
            {
                Sent(this, EventArgs.Empty);
            }
        }

        public event EventHandler Sent;
    }
}
