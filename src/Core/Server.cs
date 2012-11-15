using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lucky.Home.Core
{
    class Server : ServiceBase, IServer, IDisposable
    {
        private Dictionary<Guid, Peer> _peersByGuid = new Dictionary<Guid, Peer>();
        private Dictionary<IPAddress, Peer> _peersByAddress = new Dictionary<IPAddress, Peer>();

        private int DefaultPort = 17008;
        private TcpListener _serviceListener;

        public Server()
        {
            // Find the public IP
            HostAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));
            if (HostAddress == null)
            {
                throw new InvalidOperationException("Cannot find a public IP address of the host");
            }
            ServicePort = DefaultPort;

            _serviceListener = TryCreateListener();
            _serviceListener.Start();

            AsyncCallback handler = null;
            handler = ar =>
                    {
                        var tcpClient = _serviceListener.EndAcceptTcpClient(ar);
                        HandleServiceSocketAccepted(tcpClient);
                        _serviceListener.BeginAcceptTcpClient(handler, null);
                    };
            _serviceListener.BeginAcceptTcpClient(handler, null);
            Logger.Log("Opened Server", "host", HostAddress, "Port", ServicePort);

            // Start HELLO listener
            var listener = Manager.GetService<IHelloListener>();
            listener.PeerDiscovered += (o, e) => HandleNewPeer(e.Peer);
        }

        private void HandleNewPeer(Peer peer)
        {
            // Store it.
            // Exists same IP?
            if (_peersByAddress.ContainsKey(peer.Address))
            {
                Logger.Log("DuplicatedAddress", "address", peer.Address);
            }
            _peersByAddress[peer.Address] = peer;
            // Exists same GUID?
            if (_peersByGuid.ContainsKey(peer.ID))
            {
                Logger.Log("DuplicatedID", "guid", peer.ID);
            }
            _peersByGuid[peer.ID] = peer;
        }

        private TcpListener TryCreateListener()
        {
            do
            {
                try
                {
                    return new TcpListener(HostAddress, ServicePort);
                }
                catch (SocketException)
                {
                    Logger.Log("TCPPortBusy", "port", ServicePort, "trying", ServicePort + 1);
                    ServicePort++;
                }
            } while (true);
        }

        public void Dispose()
        {
            _serviceListener.Stop();
        }

        #region IServer interface implementation 

        /// <summary>
        /// Get the public host address
        /// </summary>
        public IPAddress HostAddress { get; private set; }

        /// <summary>
        /// Get the public host service port (TCP)
        /// </summary>
        public int ServicePort { get; private set; }

        #endregion

        private void HandleServiceSocketAccepted(TcpClient tcpClient)
        {
            using (Stream stream = tcpClient.GetStream())
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        IPAddress peerAddress = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
                        Peer peer;
                        ServerErrorCode errorCode = ServerErrorCode.Ok;
                        byte[] response = null;

                        if (!_peersByAddress.TryGetValue(peerAddress, out peer))
                        {
                            // Unknown peer!
                            Logger.Log("UnknownPeerAddress", "address", peerAddress);
                            errorCode = ServerErrorCode.UnknownAddress;
                        }
                        else
                        {
                            // Read service command
                            byte[] msgB = reader.ReadBytes(4);
                            string msg = ASCIIEncoding.ASCII.GetString(msgB);

                            switch (msg)
                            {
                                case "RGST":
                                    ReadSinkData(peer, reader, ref errorCode, ref response);
                                    break;
                                default:
                                    // ERROR, unknown command
                                    Logger.Log("UnknownCommand", "cmd", msg);
                                    errorCode = ServerErrorCode.UnknownMessage;
                                    break;
                            }
                        }

                        // Write errCode
                        writer.Write((short)errorCode);
                        if (response != null)
                        {
                            writer.Write(response);
                        }
                    }
                }
            }
            tcpClient.Close();
        }

        /// <summary>
        /// Peer started, says device capatibilities
        /// </summary>
        private void ReadSinkData(Peer peer, BinaryReader reader, ref ServerErrorCode errorCode, ref byte[] responseData)
        {
            int n = reader.ReadInt16();
            for (int i = 0; i < n; i++)
            {
                int deviceId = reader.ReadInt16();
                short deviceCaps = reader.ReadInt16();
                int port = reader.ReadInt16();

                Sink sink = Manager.GetService<SinkManager>().CreateSink(deviceId);
                if (sink == null)
                {
                    // Device id unknown!
                    errorCode = ServerErrorCode.UnknownSinkType;
                    continue;
                }

                sink.Initialize(peer, deviceCaps, port);
                peer.Sinks.Add(sink);
            }

            if (peer.ID == Guid.Empty)
            {
                // Assign new GUID
                errorCode = ServerErrorCode.AssignGuid;
                Guid guid = Guid.NewGuid();
                responseData = guid.ToByteArray();

                Logger.Log("AssignedNewID", "id", guid, "address", peer.Address);
            }
        }
    }
}
