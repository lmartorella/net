﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Lucky.Home.Core.Serialization;

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
        private const int HeloProtocolPort = 17007;
        private const int HomeProtocolPort = 17008;

        private readonly object _lock = new object();
        
        public HelloListener()
        {
            _client = new UdpClient(HeloProtocolPort, AddressFamily.InterNetwork);
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
                    Guid peerGuid;
                    bool validMsg = DecodeHelloMessage(reader, remoteEndPoint, out peerGuid);
                    if (validMsg)
                    {
                        Logger.Log("PeerDiscovered", "ID", peerGuid);
                        if (PeerDiscovered != null)
                        {
                            PeerDiscovered(this, new PeerDiscoveredEventArgs(new Peer(peerGuid, remoteEndPoint.Address)));
                        }
                        SendAck(remoteEndPoint.Address);
                    }
                }
            }

            // Enqueue again
            _client.BeginReceive(OnReceiveData, null);
        }

        private bool DecodeHelloMessage(BinaryReader reader, IPEndPoint remoteEndPoint, out Guid guid)
        {
            guid = Guid.Empty;
            try
            {
                HeloMessage msg = NetSerializer<HeloMessage>.Read(reader);
                if (msg.Preamble != HeloMessage.PreambleValue)
                {
                    return false;
                }
                guid = msg.DeviceId;

                // Good message
                return true;
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

        private void SendAck(IPAddress address)
        {
            lock (_lock)
            {
                IServer server = Manager.GetService<IServer>();

                // Forge a HERE packet
                HeloAckMessage msg = new HeloAckMessage();
                msg.Preamble = HeloAckMessage.PreambleValue;
                msg.ServerAddress = server.Address;
                msg.ServerPort = (ushort)server.Port;

                // Send a HERE packet
                using (MemoryStream stream = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        NetSerializer<HeloAckMessage>.Write(msg, writer);
                        writer.Flush();

                        var sender = new UdpClient(AddressFamily.InterNetwork);
                        IPEndPoint endPoint = new IPEndPoint(address, HomeProtocolPort);
                        sender.Send(stream.GetBuffer(), (int)stream.Length, endPoint);
                    }
                }
            }
        }
    }
}
