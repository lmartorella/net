using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class NodeManager : ServiceBase, INodeManager
    {
        private readonly Dictionary<Guid, TcpNode> _nodes = new Dictionary<Guid, TcpNode>();
        private readonly Dictionary<TcpNodeAddress, TcpNode> _unnamedNodes = new Dictionary<TcpNodeAddress, TcpNode>();
        private readonly object _nodeLock = new object();

        /// <summary>
        /// A node is discovered through heartbeat
        /// </summary>
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
                TcpNode node = FindById(guid);
                if (node == null)
                {
                    // New node!
                    _nodes.Add(guid, new TcpNode(guid, address));
                }
                else
                {
                    // The node was reset (Whatchdog?)
                    node.Relogin(address);
                }
            }
        }

        private void RegisterNewNode(TcpNodeAddress address)
        {
            TcpNode newNode;
            lock (_nodeLock)
            {
                // Ignore consecutive messages
                if (_unnamedNodes.ContainsKey(address))
                {
                    return;
                }
                newNode = new TcpNode(Guid.Empty, address);
                _unnamedNodes.Add(address, newNode);
            }
        }

        public void HeartbeatNode(Guid guid, TcpNodeAddress address)
        {
            lock (_nodeLock)
            {
                TcpNode node;
                if (guid != Guid.Empty)
                {
                    node = FindById(guid);
                }
                else
                {
                    node = FindUnnamed(address);
                }

                // Not known?
                if (node == null)
                {
                    // The server was reset?
                    RegisterNode(guid, address);
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
                    _nodes[guid] = new TcpNode(guid, address);
                }
                else
                {
                    // Refetch children
                    // TODO: deregister old child nodes
                    node.Relogin(address);
                }
            }
        }

        public Guid CreateNewGuid()
        {
            // Avoid 55aa string
            Guid ret;
            do
            {
                ret = Guid.NewGuid();
            } while (ret.ToString().ToLower().Contains("55aa") || ret.ToString().ToLower().Contains("55-aa"));
            return ret;
        }

        private TcpNode FindById(Guid guid)
        {
            TcpNode node;
            _nodes.TryGetValue(guid, out node);
            return node;
        }

        private TcpNode FindUnnamed(TcpNodeAddress address)
        {
            TcpNode node;
            _unnamedNodes.TryGetValue(address, out node);
            return node;
        }

        ITcpNode INodeManager.FindNode(Guid guid)
        {
            lock (_nodeLock)
            {
                return FindById(guid);
            }
        }

        ITcpNode INodeManager.FindNode(TcpNodeAddress address)
        {
            return FindUnnamed(address);
        }

        public IEnumerable<ITcpNode> Nodes
        {
            get
            {
                lock (_nodeLock)
                {
                    return _nodes.Values.Concat(_unnamedNodes.Values).ToArray();                    
                }
            }
        }

        public void BeginRenameNode(TcpNode node, Guid newId)
        {
            lock (_nodeLock)
            {
                if (_nodes.ContainsKey(newId))
                {
                    throw new InvalidOperationException("Node already present");
                }
                _nodes.Add(newId, node);
            }
        }

        public void EndRenameNode(TcpNode node, Guid oldId, Guid newId, bool success)
        {
            lock (_nodeLock)
            {
                if (!success)
                {
                    Debug.Assert(oldId == Guid.Empty || _nodes[oldId] == node);
                    Debug.Assert(oldId != Guid.Empty || _unnamedNodes[node.Address] == node);
                    Debug.Assert(oldId == node.Id);

                    // Cancel remove
                    _nodes.Remove(newId);
                }
                else
                {
                    Debug.Assert(oldId == Guid.Empty || _nodes[oldId] == node);
                    Debug.Assert(oldId != Guid.Empty || _unnamedNodes[node.Address] == node);
                    Debug.Assert(newId == node.Id);

                    // Remove old id
                    if (oldId != Guid.Empty)
                    {
                        _nodes.Remove(oldId);
                    }
                    else
                    {
                        _unnamedNodes.Remove(node.Address);
                    }
                }
            }
        }
    }
}
