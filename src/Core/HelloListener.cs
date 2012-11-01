using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Lucky.Home.Core
{
    /// <summary>
    /// HELLO UDP service, for discovering devices
    /// </summary>
    class HelloListener : ServiceBase, IHelloListener, IDisposable
    {
        private UdpClient _client;

        /// <summary>
        /// The HELLO port cannot be changed and are part of the protocol
        /// </summary>
        private const int HomeProtocolPort = 17007;
        private const string HelloPreamble = "HOMEHELO";
        private const string AckPreamble = "HOMEHERE";
        private const int GuidLength = 16;

        private Timer _timer;
        private readonly object _lock = new object();
        
        public HelloListener()
        {
            _client = new UdpClient(HomeProtocolPort, AddressFamily.InterNetwork);
            _client.BeginReceive(OnReceiveData, null);
        }

        private void OnReceiveData(IAsyncResult result)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(0, 0);
            byte[] bytes = _client.EndReceive(result, ref remoteEndPoint);
            if (bytes.Length > 0 && !IPAddress.IsLoopback(remoteEndPoint.Address))
            {
                Guid peerGuid;
                bool validMsg = DecodeHelloMessage(bytes, remoteEndPoint, out peerGuid);
                if (validMsg)
                {
                    Logger.Log("PeerDiscovered", "ID", peerGuid);
                    if (PeerDiscovered != null)
                    {
                        PeerDiscovered(this, new PeerDiscoveredEventArgs(new Peer(peerGuid, remoteEndPoint.Address)));
                    }
                    ScheduleAckMessage();
                }
            }

            // Enqueue again
            _client.BeginReceive(OnReceiveData, null);
        }

        private bool DecodeHelloMessage(byte[] bytes, IPEndPoint remoteEndPoint, out Guid guid)
        {
            guid = Guid.Empty;
            try
            {
                using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes), ASCIIEncoding.ASCII))
                {
                    string preamble = new string(reader.ReadChars(HelloPreamble.Length));
                    if (preamble != HelloPreamble)
                    {
                        return false;
                    }
                    guid = new Guid(reader.ReadBytes(GuidLength));

                    // Good message
                    return true;
                }
            }
            catch (EndOfStreamException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            _client.Close();
        }

        /// <summary>
        /// Event raised when a new peer is started/powered and says HELLO
        /// </summary>
        public event EventHandler<PeerDiscoveredEventArgs> PeerDiscovered;

        private void ScheduleAckMessage()
        {
            // If not scheduled yet, starts a 1 sec. timer
            lock (_lock)
            {
                if (_timer == null)
                {
                    _timer = new Timer(HandleAckTimer, null, TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(-1));
                }
            }
        }

        private void HandleAckTimer(object state)
        {
            lock (_lock)
            {
                _timer = null;

                IServer server = Manager.GetService<IServer>();

                // Forge a HERE packet
                MemoryStream ms = new MemoryStream();
                using (BinaryWriter writer = new BinaryWriter(ms, ASCIIEncoding.ASCII))
                {
                    writer.Write(ASCIIEncoding.ASCII.GetBytes(AckPreamble));
                    writer.Write(server.HostAddress.GetAddressBytes());
                    writer.Write((short)server.ServicePort);
                }

                // Send a HERE packet
                var sender = new UdpClient(AddressFamily.InterNetwork);
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, HomeProtocolPort + 1);

                // Send HERE packet
                byte[] bytes = ms.GetBuffer();
                sender.Send(bytes, bytes.Length, endPoint);
            }
        }
    }
}
