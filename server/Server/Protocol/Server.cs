using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Lucky.Net;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    class Server : ServiceBase, IServer
    {
        private readonly TcpListener[] _tcpListeners;
        private UdpControlPortListener[] _helloListeners;
        private readonly INodeManager _nodeManager;

        // Find a free TCP port
        private const int DEFAULT_PORT = 17010;

        public Server() 
        {
            // Find the public IP
            Addresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)).ToArray();
            if (!Addresses.Any())
            {
                Addresses = new[] { new IPAddress(0x0100007f) };
                Logger.Warning("Cannot find a valid public IP address of the host. Using IPv4 loopback.");
            }
            //Port = DefaultPort;
            _nodeManager = Manager.GetService<INodeManager>();

            _helloListeners = Addresses.Select(address =>
            {
                // Start HELLO listener
                var helloListener = new UdpControlPortListener(address);
                helloListener.NodeMessage += (o, e) => HandleNodeMessage(e.Guid, e.Address, e.MessageType);
                return helloListener;
            }).ToArray();

            _tcpListeners = Addresses.Select(address => Manager.GetService<TcpService>().CreateListener(address, DEFAULT_PORT, "HomeServer", HandleServiceSocketAccepted)).ToArray();

            Logger.Log("Opened Server", "hosts", string.Join(";", Addresses.Select(a => a.ToString())));
        }

        public override void Dispose()
        {
            foreach (var serviceListener in _helloListeners)
            {
                serviceListener.Dispose();
            }
            _helloListeners = new UdpControlPortListener[0];
        }

        private void HandleNodeMessage(Guid guid, TcpNodeAddress address, PingMessageType messageType)
        {
            // Messages from level-0
            switch (messageType)
            {
                case PingMessageType.Hello:
                    _nodeManager.RegisterNode(guid, address);
                    break;
                case PingMessageType.Heartbeat:
                    _nodeManager.HeartbeatNode(guid, address);
                    break;
                case PingMessageType.SubNodeChanged:
                    _nodeManager.RefetchSubNodes(guid, address);
                    break;
            }
        }

        #region IServer interface implementation

        /// <summary>
        /// Get the public host address
        /// </summary>
        public IPAddress[] Addresses { get; private set; }

        #endregion

        private void HandleServiceSocketAccepted(NetworkStream stream)
        {
            using (new BinaryReader(stream))
            {
                throw new NotSupportedException("Tcp port not supported yet");
            }
        }
    }
}
