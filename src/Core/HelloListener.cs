using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Lucky.Home.Core.Serialization;

namespace Lucky.Home.Core
{
    /// <summary>
    /// HELLO UDP service, for discovering devices
    /// </summary>
    class HelloListener : IDisposable
    {
        private readonly IPAddress _address;
        private readonly UdpClient _client;

        /// <summary>
        /// The HELLO port cannot be changed and are part of the protocol
        /// </summary>
        private const int HeloProtocolPort = 17007;

        private readonly object _lock = new object();
        private readonly Logger _logger;

        public HelloListener(IPAddress address)
        {
            _address = address;
            _logger = new Logger();
            _client = new UdpClient(new IPEndPoint(address, HeloProtocolPort));
            _client.BeginReceive(OnReceiveData, null);
        }

        private void OnReceiveData(IAsyncResult result)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(0, 0);
            byte[] bytes = _client.EndReceive(result, ref remoteEndPoint);
            if (bytes.Length > 0 && !IPAddress.IsLoopback(remoteEndPoint.Address))
            {
                using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes)))
                {
                    HeloMessage peerMsg;
                    bool validMsg = DecodeHelloMessage(reader, out peerMsg);
                    if (validMsg)
                    {
                        _logger.Log("PeerDiscovered", "ID", peerMsg.DeviceId);
                        if (PeerDiscovered != null)
                        {
                            PeerDiscovered(this, new PeerDiscoveredEventArgs(new Peer(peerMsg.DeviceId, remoteEndPoint.Address)));
                        }
                        SendAck(remoteEndPoint.Address, _address, peerMsg.AckPort);
                    }
                }
            }

            // Enqueue again
            _client.BeginReceive(OnReceiveData, null);
        }

        private bool DecodeHelloMessage(BinaryReader reader, out HeloMessage msg)
        {
            try
            {
                msg = NetSerializer<HeloMessage>.Read(reader);
                if (msg.Preamble != HeloMessage.PreambleValue)
                {
                    return false;
                }
                // Good message
                return true;
            }
            catch (EndOfStreamException)
            {
                msg = null;
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

        private void SendAck(IPAddress address, IPAddress hostAddress, int ackPort)
        {
            lock (_lock)
            {
                IServer server = Manager.GetService<IServer>();

                // Forge a HERE packet
                HeloAckMessage msg = new HeloAckMessage();
                msg.Preamble = HeloAckMessage.PreambleValue;
                msg.ServerAddress = hostAddress;
                msg.ServerPort = server.Port;

                // Send a HERE packet
                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        NetSerializer<HeloAckMessage>.Write(msg, writer);
                        writer.Flush();

                        var sender = new UdpClient(AddressFamily.InterNetwork);
                        IPEndPoint endPoint = new IPEndPoint(address, ackPort);
                        sender.Send(stream.GetBuffer(), (int)stream.Length, endPoint);
                    }
                }
            }
        }
    }
}
