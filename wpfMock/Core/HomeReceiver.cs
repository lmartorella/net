using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lucky.HomeMock.Core
{
    class HomeReceiver : Task
    {
        private UdpClient _client;

        public HomeReceiver()
        {
            _client = new UdpClient(17007, AddressFamily.InterNetwork);
            _client.BeginReceive(OnReceiveData, null);
        }

        public override void Dispose()
        {
            _client.Close();
            base.Dispose();
        }

        private void OnReceiveData(IAsyncResult result)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(0, 0);
            byte[] bytes = _client.EndReceive(result, ref remoteEndPoint);
            if (bytes.Length > 0 && !IPAddress.IsLoopback(remoteEndPoint.Address))
            {
                if (DecodeHomeMessage(bytes))
                {
                    if (HomeFound != null)
                    {
                        HomeFound(this, EventArgs.Empty);
                    }
                }
            }

            // Enqueue again
            _client.BeginReceive(OnReceiveData, null);
        }

        public event EventHandler HomeFound;

        public IPAddress HomeHost { get; private set; }
        public int HomePort { get; private set; }

        private bool DecodeHomeMessage(byte[] bytes)
        {
            try
            {
                using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes), ASCIIEncoding.ASCII))
                {
                    const string HomePreamble = "HOMEHERE0";
                    string preamble = new string(reader.ReadChars(HomePreamble.Length));
                    if (preamble != HomePreamble)
                    {
                        return false;
                    }

                    // Read ip address
                    HomeHost = new IPAddress(reader.ReadBytes(4));
                    HomePort = reader.ReadInt16();

                    // Good message
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }
    }
}
