using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Lucky.Home.Core;
using Lucky.Home.Net;

namespace Lucky.Home.Protocol
{
    class Server : ServiceBase, IServer
    {
        private readonly TcpListener[] _tcpListeners;
        private UdpControlPortListener[] _helloListeners;
        private readonly INodeRegistrar _nodeRegistrar;

        // Find a free TCP port
        private const int DEFAULT_PORT = 17010;

        public Server() 
        {
            // Find the public IP
            Addresses = Dns.GetHostAddresses(Dns.GetHostName()).Where(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)).ToArray();
            if (!Addresses.Any())
            {
                throw new InvalidOperationException("Cannot find a valid public IP address of the host");
            }
            //Port = DefaultPort;
            _nodeRegistrar = Manager.GetService<INodeRegistrar>();

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
                    _nodeRegistrar.RegisterNode(guid, address);
                    break;
                case PingMessageType.Heartbeat:
                    _nodeRegistrar.HeartbeatNode(guid, address);
                    break;
                case PingMessageType.SubNodeChanged:
                    _nodeRegistrar.RefetchSubNodes(guid, address);
                    break;
            }
        }

        #region IServer interface implementation

        /// <summary>
        /// Get the public host address
        /// </summary>
        public IPAddress[] Addresses { get; private set; }

        #endregion

        private void HandleServiceSocketAccepted(TcpClient tcpClient)
        {
            //IPAddress peerAddress = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
            using (Stream stream = tcpClient.GetStream())
            {
                using (new BinaryReader(stream))
                {
                    throw new NotSupportedException("Tcp port not supported yet");
                }
            }
            //tcpClient.Close();
        }
    }
}
