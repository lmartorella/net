using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lucky.Home.Core
{
    /// <summary>
    /// HELLO UDP service, for discovering devices
    /// </summary>
    class HelloListener : IHelloListener, IDisposable
    {
        private UdpClient _client;
        private const int ServerPort = 17007;
        private const string Preamble = "HOMEHELO";
        private const int GuidLength = 16;
        
        public HelloListener()
        {
            _client = new UdpClient(ServerPort, AddressFamily.InterNetwork);
            _client.BeginReceive(OnReceiveData, null);
        }

        private void OnReceiveData(IAsyncResult result)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(0, 0);
            byte[] bytes = _client.EndReceive(result, ref remoteEndPoint);
            if (bytes.Length > 0)
            {
                Peer peer = DecodeHelloMessage(bytes, remoteEndPoint);
                if (peer != null)
                {
                    Logger.Log("PeerDiscovered", "ID", peer.ID);
                }
            }

            // Enqueue again
            _client.BeginReceive(OnReceiveData, null);
        }

        private ILogger Logger
        {
            get
            {
                return Manager.GetService<ILogger>();
            }
        }

        private Peer DecodeHelloMessage(byte[] bytes, IPEndPoint remoteEndPoint)
        {
            if (bytes.Length >= Preamble.Length + 1 + GuidLength)
            {
                if (ASCIIEncoding.Default.GetString(bytes, 0, Preamble.Length) == Preamble)
                {
                    byte[] g = new byte[GuidLength];
                    Array.Copy(bytes, Preamble.Length + 1, g, 0, GuidLength);
                    Guid guid = new Guid(bytes);
                    int ver = bytes[Preamble.Length] - '0';
                    if (ver > 0)
                    {
                        Logger.Log("PeerUnsupported", "ID", guid, "ver", ver);
                        return null;
                    }

                    return new Peer(guid, remoteEndPoint.Address, remoteEndPoint.Port);
                }
            }
            return null;
        }

        public void Dispose()
        {
            _client.Close();
        }

        /// <summary>
        /// Event raised when a new peer is started/powered and requires registration
        /// </summary>
        public event EventHandler<PeerDiscoveredEventArgs> PeerDiscovered;
    }
}
