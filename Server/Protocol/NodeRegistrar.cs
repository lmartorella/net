using System;
using System.Collections.Generic;
using System.Linq;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class NodeRegistrar : ServiceBase, INodeRegistrar
    {
        private readonly Dictionary<Guid, TcpNode> _nodes = new Dictionary<Guid, TcpNode>();
        private readonly HashSet<TcpNodeAddress> _addressInRegistration = new HashSet<TcpNodeAddress>();
        private readonly object _nodeLock = new object();

        public void RegisterNode(Guid guid, TcpNodeAddress address)
        {
            TcpNode node;
            if (guid == Guid.Empty)
            {
                node = RegisterNewNode(address);
            }
            else
            {
                node = RegisterNamedNode(guid, address);
            }

            if (node != null)
            {
                if (node.ShouldBeRenamed)
                {
                    node.Rename(node.Id);
                }
                // Start data fetch asynchrously
                node.FetchMetadata();
            }
        }

        private TcpNode RegisterNamedNode(Guid guid, TcpNodeAddress address)
        {
            lock (_nodeLock)
            {
                TcpNode node = FindNode(guid);
                if (node == null)
                {
                    // New node!
                    _nodes[guid] = LoginNode(guid, address);
                }
                else
                {
                    // The node was reset
                    node.Relogin(address);
                }

                return node;
            }
        }

        public void HeartbeatNode(Guid guid, TcpNodeAddress address)
        {
            lock (_nodeLock)
            {
                TcpNode node;
                _nodes.TryGetValue(guid, out node);
                if (node == null)
                {
                    // The server was reset?
                    _nodes[guid] = LoginNode(guid, address);
                    // Async task
                    _nodes[guid].FetchMetadata();
                }
                else
                {
                    // Normal heartbeat
                    node.Heartbeat(address);
                }
            }
        }

        public void RefetchSubNodes(Guid guid, TcpNodeAddress address)
        {
            lock (_nodeLock)
            {
                TcpNode node;
                _nodes.TryGetValue(guid, out node);
                if (node == null)
                {
                    // The server was reset?
                    _nodes[guid] = LoginNode(guid, address);
                }
                else
                {
                    // Refetch children
                    node.RefetchChildren(address);
                }
            }
        }

        private TcpNode RegisterNewNode(TcpNodeAddress address)
        {
            lock (_nodeLock)
            {
                // Ignore consecutive messages
                if (_addressInRegistration.Contains(address))
                {
                    return null;
                }
                _addressInRegistration.Add(address);
            }
            var newNode = RegisterBlankNode(address);
            lock (_nodeLock)
            {
                _addressInRegistration.Remove(address);
                _nodes[newNode.Id] = newNode;
            }
            return newNode;
        }

        private TcpNode LoginNode(Guid guid, TcpNodeAddress address)
        {
            return new TcpNode(guid, address);
        }

        private TcpNode RegisterBlankNode(TcpNodeAddress address)
        {
            return new TcpNode(CreateNewGuid(), address, true);
        }

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

        private TcpNode FindNode(Guid guid)
        {
            TcpNode node;
            _nodes.TryGetValue(guid, out node);
            return node;
        }

        ITcpNode INodeRegistrar.FindNode(Guid guid)
        {
            TcpNode node;
            _nodes.TryGetValue(guid, out node);
            return node;
        }

        public IEnumerable<ITcpNode> Nodes
        {
            get
            {
                lock (_nodeLock)
                {
                    return _nodes.Values.ToArray();                    
                }
            }
        }
    }
}
