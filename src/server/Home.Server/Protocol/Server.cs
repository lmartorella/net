using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Lucky.Net;
using Lucky.Home.Services;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// The server logic
    /// </summary>
    class Server : ServiceBase
    {
        private UdpControlPortListener[] _helloListeners;
        private readonly NodeManager _nodeManager;

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
            _nodeManager = Manager.GetService<NodeManager>();

            _helloListeners = Addresses.Select(address =>
            {
                // Start HELLO listener
                var helloListener = new UdpControlPortListener(address);
                helloListener.NodeMessage += (o, e) => HandleNodeMessage(e.NodeId, e.Address, e.MessageType, e.ChildrenChanged);
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
            _helloListeners = new UdpControlPortListener[0];
        }

        private async void HandleNodeMessage(NodeId id, TcpNodeAddress address, PingMessageType messageType, int[] childrenChanged)
        {
            // Messages from level-0
            switch (messageType)
            {
                case PingMessageType.Hello:
                    await _nodeManager.RegisterNode(id, address);
                    break;
                case PingMessageType.Heartbeat:
                    await _nodeManager.HeartbeatNode(id, address);
                    break;
                case PingMessageType.SubNodeChanged:
                    await _nodeManager.RefetchSubNodes(id, address, childrenChanged);
                    break;
            }
        }

        #region IServer interface implementation

        /// <summary>
        /// Get the public host address
        /// </summary>
        public IPAddress[] Addresses { get; private set; }

        #endregion
    }
}
