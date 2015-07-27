using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Home.Core;

namespace Lucky.Home.Protocol
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class NodeRegistrar : ServiceBase, INodeRegistrar
    {
        private readonly Dictionary<Guid, ITcpNode> _nodes = new Dictionary<Guid, ITcpNode>();
        private readonly HashSet<TcpNodeAddress> _addressInRegistration = new HashSet<TcpNodeAddress>();
        private readonly object _nodeLock = new object();

        public void RegisterNode(Guid guid, TcpNodeAddress address)
        {
            if (guid == Guid.Empty)
            {
                RegisterNewNode(address);
            }
            else
            {
                RegisterNamedNode(guid, address);
            }
        }

        private void RegisterNamedNode(Guid guid, TcpNodeAddress address)
        {
            lock (_nodeLock)
            {
                ITcpNode node;
                _nodes.TryGetValue(guid, out node);
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
            }
        }

        public void HeartbeatNode(Guid guid, TcpNodeAddress address)
        {
            lock (_nodeLock)
            {
                ITcpNode node;
                _nodes.TryGetValue(guid, out node);
                if (node == null)
                {
                    // The server was reset?
                    _nodes[guid] = LoginNode(guid, address);
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
                ITcpNode node;
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

        private async void RegisterNewNode(TcpNodeAddress address)
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
            var newNode = await RegisterBlankNode(address);
            lock (_nodeLock)
            {
                _addressInRegistration.Remove(address);
                _nodes[newNode.Id] = newNode;
            }
        }

        private ITcpNode LoginNode(Guid guid, TcpNodeAddress address)
        {
            var node = new TcpNode(guid, address);
            // Start data fetch asynchrously
            node.FetchSinkData();
            return node;
        }

        private async Task<ITcpNode> RegisterBlankNode(TcpNodeAddress address)
        {
            var node = new TcpNode(CreateNewGuid(), address);
            // Give the node the new name
            await node.Rename();
            // Start data fetch asynchrously
            node.FetchSinkData();
            return node;
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

        public ITcpNode FindNode(Guid guid)
        {
            ITcpNode node;
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
