using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Lucky.Home.Core.Serialization;

namespace Lucky.Home.Core
{
    class Server : ServiceBase, IServer, IDisposable
    {
        private readonly Dictionary<Guid, Peer> _peersByGuid = new Dictionary<Guid, Peer>();
        private readonly Dictionary<IPAddress, Peer> _peersByAddress = new Dictionary<IPAddress, Peer>();

        private const ushort DefaultPort = 17010;
        private readonly TcpListener[] _serviceListeners;

        public Server()
        {
            // Find the public IP
            Addresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)).ToArray();
            if (!Addresses.Any())
            {
                throw new InvalidOperationException("Cannot find a valid public IP address of the host");
            }
            Port = DefaultPort;

            _serviceListeners = Addresses.Select(address =>
            {
                TcpListener listener = TryCreateListener(address);
                listener.Start();
                AsyncCallback handler = null;
                handler = ar =>
                {
                    var tcpClient = listener.EndAcceptTcpClient(ar);
                    HandleServiceSocketAccepted(tcpClient);
                    listener.BeginAcceptTcpClient(handler, null);
                };
                listener.BeginAcceptTcpClient(handler, null);
                
                // Start HELLO listener
                var helloListener = new HelloListener(address);
                helloListener.PeerDiscovered += (o, e) => HandleNewPeer(e.Peer);

                return listener;
            }).ToArray();

            Logger.Log("Opened Server", "hosts", string.Join(";", Addresses.Select(a => a.ToString())), "Port", Port);
        }

        private void HandleNewPeer(Peer peer)
        {
            // Store it.
            // Exists same IP?
            Peer oldPeer;
            if (_peersByAddress.TryGetValue(peer.Address, out oldPeer))
            {
                if (!oldPeer.Equals(peer))
                {
                    Logger.Log("DuplicatedAddress", "address", peer.Address);
                }
            }

            _peersByAddress[peer.Address] = peer;

            // Exists same GUID?
            if (_peersByGuid.TryGetValue(peer.ID, out oldPeer))
            {
                if (!oldPeer.Equals(peer))
                {
                    Logger.Log("DuplicatedID", "guid", peer.ID);
                }
            }
            _peersByGuid[peer.ID] = peer;
        }

        private TcpListener TryCreateListener(IPAddress address)
        {
            do
            {
                try
                {
                    return new TcpListener(address, Port);
                }
                catch (SocketException)
                {
                    Logger.Log("TCPPortBusy", "port", address + ":" + Port, "trying", Port + 1);
                    Port++;
                }
            } while (true);
        }

        public void Dispose()
        {
            foreach (var serviceListener in _serviceListeners)
            {
                serviceListener.Stop();
            }
        }

        #region IServer interface implementation 

        /// <summary>
        /// Get the public host address
        /// </summary>
        public IPAddress[] Addresses { get; private set; }

        /// <summary>
        /// Get the public host service port (TCP)
        /// </summary>
        public ushort Port { get; private set; }

        #endregion

        private void HandleServiceSocketAccepted(TcpClient tcpClient)
        {
            using (Stream stream = tcpClient.GetStream())
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    IPAddress peerAddress = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
                    Peer peer;
                    ServiceResponse response = new ServiceResponse();
                    response.ErrCode = ServerErrorCode.Ok;

                    if (!_peersByAddress.TryGetValue(peerAddress, out peer))
                    {
                        // Unknown peer!
                        Logger.Log("UnknownPeerAddress", "address", peerAddress);
                        response.ErrCode = ServerErrorCode.UnknownAddress;
                    }
                    else
                    {
                        ServiceCommand cmd = NetSerializer<ServiceCommand>.Read(reader);
                        if (cmd is ServiceRegisterCommand)
                        {
                            ReadSinkData(peer, (ServiceRegisterCommand)cmd, ref response);
                        }
                        else
                        {
                            // ERROR, unknown command
                            Logger.Log("UnknownCommand", "cmd", cmd.Command);
                            response.ErrCode = ServerErrorCode.UnknownMessage;
                        }
                    }

                    // Write response
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        NetSerializer<ServiceResponse>.Write(response, writer);
                    }
                }
            }
            tcpClient.Close();
        }

        /// <summary>
        /// Peer started, says device capatibilities
        /// </summary>
        private void ReadSinkData(Peer peer, ServiceRegisterCommand cmd, ref ServiceResponse responseData)
        {
            foreach (var sinkInfo in cmd.Sinks)
            {
                Sink sink = Manager.GetService<SinkManager>().CreateSink(sinkInfo.SinkType);
                if (sink == null)
                {
                    // Device id unknown!
                    responseData.ErrCode = ServerErrorCode.UnknownSinkType;
                    continue;
                }

                sink.Initialize(peer, sinkInfo.DeviceCaps, sinkInfo.Port);
                peer.Sinks.Add(sink);
            }

            if (peer.ID == Guid.Empty)
            {
                responseData = new ServiceResponseWithGuid();
                // Assign new GUID
                responseData.ErrCode = ServerErrorCode.AssignGuid;
                var guid = ((ServiceResponseWithGuid)responseData).NewGuid = Guid.NewGuid();

                Logger.Log("AssignedNewID", "id", guid, "address", peer.Address);
            }
        }
    }
}
