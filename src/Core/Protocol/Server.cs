using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Lucky.Home.Core.Protocol
{
    class Server : ServiceBase, IServer
    {
        private readonly Dictionary<Guid, INode> _nodes = new Dictionary<Guid, INode>();
        private readonly List<IPAddress> _addressInRegistration = new List<IPAddress>();

        private readonly TcpListener[] _tcpListeners;
        private ControlPortListener[] _helloListeners;
        private readonly INodeRegistrar _nodeRegistrar;
        private readonly object _nodeLock = new object();

        // Find a free TCP port
        private int _tcpPort = 17010;

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
            _nodeRegistrar = Manager.GetService<INodeRegistrar>();

            _helloListeners = Addresses.Select(address =>
            {
                // Start HELLO listener
                var helloListener = new ControlPortListener(address);
                helloListener.NodeMessage += (o, e) => HandleNodeMessage(e.Guid, e.Address, e.IsNew);
                return helloListener;
            }).ToArray();

            _tcpListeners = Addresses.Select(address =>
            {
                // Start TCP listener
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
                return listener;
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
            if (isNew)
            {
                // Check if a new empty GUID is about to register to the system
                if (guid == Guid.Empty)
                {
                    RegisterNewNode(address);
                }
                else
                {
                    RegisterNamedNode(guid, address);
                }
            }
            else
            {
                HeartbeatNode(guid, address);
            }
        }

        private void RegisterNamedNode(Guid guid, IPAddress address)
        {
            lock (_nodeLock)
            {
                INode node;
                _nodes.TryGetValue(guid, out node);
                if (node == null)
                {
                    // New node!
                    _nodes[guid] = _nodeRegistrar.LoginNode(guid, address);
                }
                else
                {
                    // The node was reset
                    node.Relogin(address);
                }
            }
        }

        private void HeartbeatNode(Guid guid, IPAddress address)
        {
            lock (_nodeLock)
            {
                INode node;
                _nodes.TryGetValue(guid, out node);
                if (node == null)
                {
                    // The server was reset?
                    _nodes[guid] = _nodeRegistrar.LoginNode(guid, address);
                }
                else
                {
                    // Normal heartbeat
                    node.Heartbeat(address);
                }
            }
        }

        private async void RegisterNewNode(IPAddress address)
        {
            lock (_nodeLock)
            {
                // Ignore consecutive messages
                if (_addressInRegistration.Contains(address))
                {
                    return;
                }
                _addressInRegistration.Add(address);
            }
            var newNode = await _nodeRegistrar.RegisterBlankNode(address);
            lock (_nodeLock)
            {
                _addressInRegistration.Remove(address);
                _nodes[newNode.Id] = newNode;
            }
        }

        private TcpListener TryCreateListener(IPAddress address)
        {
            do
            {
                try
                {
                    return new TcpListener(address, _tcpPort);
                }
                catch (SocketException)
                {
                    Logger.Log("TCPPortBusy", "port", address + ":" + _tcpPort, "trying", _tcpPort + 1);
                    _tcpPort++;
                }
            } while (true);
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
