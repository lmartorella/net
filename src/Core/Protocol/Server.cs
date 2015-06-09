using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Lucky.Home.Core.Protocol
{
    class Server : ServiceBase, IServer
    {
        //private readonly Dictionary<Guid, Node> _nodesByGuid = new Dictionary<Guid, Node>();
        //private readonly Dictionary<IPAddress, Node> _peersByAddress = new Dictionary<IPAddress, Node>();

        //private const ushort DefaultPort = 17010;
        //private readonly TcpListener[] _serviceListeners;
        private ControlPortListener[] _helloListeners;

        public Server() 
            :base("Server")
        {
            // Find the public IP
            Addresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)).ToArray();
            if (!Addresses.Any())
            {
                throw new InvalidOperationException("Cannot find a valid public IP address of the host");
            }
            //Port = DefaultPort;

            _helloListeners = Addresses.Select(address =>
            {
                //TcpListener listener = TryCreateListener(address);
                //listener.Start();
                //AsyncCallback handler = null;
                //handler = ar =>
                //{
                //    var tcpClient = listener.EndAcceptTcpClient(ar);
                //    HandleServiceSocketAccepted(tcpClient);
                //    listener.BeginAcceptTcpClient(handler, null);
                //};
                //listener.BeginAcceptTcpClient(handler, null);

                //return listener;

                // Start HELLO listener
                var helloListener = new ControlPortListener(address);
                helloListener.NodeMessage += (o, e) => HandleNodeMessage(e.Guid, e.Address, e.IsNew);
                return helloListener;
            }).ToArray();

            Logger.Log("Opened Server", "hosts", string.Join(";", Addresses.Select(a => a.ToString())));
        }

        public override void Dispose()
        {
            foreach (var serviceListener in _helloListeners)
            {
                serviceListener.Dispose();
            }
            _helloListeners = new ControlPortListener[0];
        }

        private void HandleNodeMessage(Guid guid, IPAddress address, bool isNew)
        {
            //// Store it.
            //// Exists same IP?
            //Peer oldPeer;
            //if (_peersByAddress.TryGetValue(peer.Address, out oldPeer))
            //{
            //    if (!oldPeer.Equals(peer))
            //    {
            //        Logger.Log("DuplicatedAddress", "address", peer.Address);
            //    }
            //}

            //_peersByAddress[peer.Address] = peer;

            //// Exists same GUID?
            //if (_nodesByGuid.TryGetValue(peer.ID, out oldPeer))
            //{
            //    if (!oldPeer.Equals(peer))
            //    {
            //        Logger.Log("DuplicatedID", "guid", peer.ID);
            //    }
            //}
            //_nodesByGuid[peer.ID] = peer;
        }

        //private TcpListener TryCreateListener(IPAddress address)
        //{
        //    do
        //    {
        //        try
        //        {
        //            return new TcpListener(address, Port);
        //        }
        //        catch (SocketException)
        //        {
        //            Logger.Log("TCPPortBusy", "port", address + ":" + Port, "trying", Port + 1);
        //            Port++;
        //        }
        //    } while (true);
        //}

        #region IServer interface implementation

        /// <summary>
        /// Get the public host address
        /// </summary>
        public IPAddress[] Addresses { get; private set; }

        ///// <summary>
        ///// Get the public host service port (TCP)
        ///// </summary>
        //public ushort Port { get; private set; }

        #endregion

        //private void HandleServiceSocketAccepted(TcpClient tcpClient)
        //{
        //    using (Stream stream = tcpClient.GetStream())
        //    {
        //        using (BinaryReader reader = new BinaryReader(stream))
        //        {
        //            IPAddress peerAddress = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
        //            Peer peer;
        //            ServiceResponse response = new ServiceResponse();
        //            response.ErrCode = ServerErrorCode.Ok;

        //            if (!_peersByAddress.TryGetValue(peerAddress, out peer))
        //            {
        //                // Unknown peer!
        //                Logger.Log("UnknownPeerAddress", "address", peerAddress);
        //                response.ErrCode = ServerErrorCode.UnknownAddress;
        //            }
        //            else
        //            {
        //                ServiceCommand cmd = NetSerializer<ServiceCommand>.Read(reader);
        //                if (cmd is ServiceRegisterCommand)
        //                {
        //                    ReadSinkData(peer, (ServiceRegisterCommand)cmd, ref response);
        //                }
        //                else
        //                {
        //                    // ERROR, unknown command
        //                    Logger.Log("UnknownCommand", "cmd", cmd.Command);
        //                    response.ErrCode = ServerErrorCode.UnknownMessage;
        //                }
        //            }

        //            // Write response
        //            using (BinaryWriter writer = new BinaryWriter(stream))
        //            {
        //                NetSerializer<ServiceResponse>.Write(response, writer);
        //            }
        //        }
        //    }
        //    tcpClient.Close();
        //}

        ///// <summary>
        ///// Peer started, says device capatibilities
        ///// </summary>
        //private void ReadSinkData(Peer peer, ServiceRegisterCommand cmd, ref ServiceResponse responseData)
        //{
        //    foreach (var sinkInfo in cmd.Sinks)
        //    {
        //        Sink sink = Manager.GetService<SinkManager>().CreateSink(sinkInfo.SinkType);
        //        if (sink == null)
        //        {
        //            // Device id unknown!
        //            responseData.ErrCode = ServerErrorCode.UnknownSinkType;
        //            continue;
        //        }

        //        sink.Initialize(peer, sinkInfo.DeviceCaps, sinkInfo.Port);
        //        peer.Sinks.Add(sink);
        //    }

        //    if (peer.ID == Guid.Empty)
        //    {
        //        responseData = new ServiceResponseWithGuid();
        //        // Assign new GUID
        //        responseData.ErrCode = ServerErrorCode.AssignGuid;
        //        var guid = ((ServiceResponseWithGuid)responseData).NewGuid = CreateNewGuid();

        //        Logger.Log("AssignedNewID", "id", guid, "address", peer.Address);
        //    }
        //}

        private static Guid CreateNewGuid()
        {
            // Avoid 55aa string
            Guid ret;
            do
            {
                ret = Guid.NewGuid();
            } while (ret.ToString().ToLower().Contains("55aa") || ret.ToString().ToLower().Contains("55-aa"));
            return ret;
        }
    }
}
