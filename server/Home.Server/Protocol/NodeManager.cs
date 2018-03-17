using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class NodeManager : ServiceBase, INodeManager
    {
        private readonly Dictionary<NodeId, TcpNode> _nodes = new Dictionary<NodeId, TcpNode>();
        private readonly Dictionary<TcpNodeAddress, TcpNode> _unnamedNodes = new Dictionary<TcpNodeAddress, TcpNode>();
        private readonly object _nodeLock = new object();

        /// <summary>
        /// A node is discovered through heartbeat
        /// </summary>
        public async Task<ITcpNode> RegisterNode(NodeId id, TcpNodeAddress address)
        {
            if (id.IsEmpty)
            {
                return await RegisterNewNode(address);
            }
            else
            {
                return await RegisterNamedNode(id, address);
            }
        }

        private async Task<ITcpNode> RegisterNamedNode(NodeId id, TcpNodeAddress address)
        {
            TcpNode node;
            bool relogin;
            lock (_nodeLock)
            {
                node = FindById(id);
                if (node == null)
                {
                    // New node!
                    node = new TcpNode(id, address);
                    _nodes.Add(id, node);
                    relogin = false;
                }
                else
                {
                    relogin = true;
                }
            }

            if (relogin)
            {
                // The node was reset (Whatchdog?)
                await node.Relogin(address);
            }
            else
            {
                await node.FetchMetadata();
            }
            return node;
        }

        private async Task<ITcpNode> RegisterNewNode(TcpNodeAddress address)
        {
            TcpNode newNode;
            lock (_nodeLock)
            {
                // Ignore consecutive messages
                if (_unnamedNodes.TryGetValue(address, out newNode))
                {
                    if (!newNode.IsZombie)
                    {
                        return newNode;
                    }
                    else
                    {
                        newNode.IsZombie = false;
                    }
                }
                else
                {
                    newNode = new TcpNode(new NodeId(), address);
                    _unnamedNodes.Add(address, newNode);
                }
            }
            await newNode.FetchMetadata();
            return newNode;
        }

        public async Task<ITcpNode> RegisterUnknownNode(TcpNodeAddress address)
        {
            // Ask for guid
            var id = new TcpNode(new NodeId(), address).TryFetchGuid();
            if (id == null)
            {
                // Error in fetching
                Logger.Warning("Error/timeout in RegisterUnknownNode of " + address);
                return null;
            }
            else
            {
                return await RegisterNode(id, address);
            }
        }

        public async Task HeartbeatNode(NodeId id, TcpNodeAddress address)
        {
            TcpNode node;
            lock (_nodeLock)
            {
                node = (!id.IsEmpty) ? FindById(id) : FindUnnamed(address);
            }

            // Not known?
            if (node == null)
            {
                // The server was reset?
                await RegisterNode(id, address);
            }
            else
            {
                // Normal heartbeat
                await node.Heartbeat(address);
            }
        }

        public async Task RefetchSubNodes(NodeId id, TcpNodeAddress address, int[] childrenChanged)
        {
            TcpNode node;
            lock (_nodeLock)
            {
                node = (!id.IsEmpty) ? FindById(id) : FindUnnamed(address);
            }

            // Not known?
            if (node == null)
            {
                // The server was reset?
                await RegisterNode(id, address);
            }
            else
            {
                // Normal heartbeat
                await node.Relogin(address, childrenChanged);
            }
        }

        private TcpNode FindById(NodeId id)
        {
            TcpNode node;
            _nodes.TryGetValue(id, out node);
            return node;
        }

        private TcpNode FindUnnamed(TcpNodeAddress address)
        {
            TcpNode node;
            _unnamedNodes.TryGetValue(address, out node);
            return node;
        }

        ITcpNode INodeManager.FindNode(NodeId id)
        {
            lock (_nodeLock)
            {
                return FindById(id);
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

        public void BeginRenameNode(TcpNode node, NodeId newId)
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

        public void EndRenameNode(TcpNode node, NodeId oldId, NodeId newId, bool success)
        {
            lock (_nodeLock)
            {
                if (!success)
                {
                    Debug.Assert(oldId.IsEmpty || _nodes[oldId] == node);
                    Debug.Assert((!oldId.IsEmpty) || _unnamedNodes[node.Address] == node);
                    Debug.Assert(oldId.Equals(node.NodeId));

                    // Cancel remove
                    _nodes.Remove(newId);
                }
                else
                {
                    Debug.Assert(oldId.IsEmpty || _nodes[oldId] == node);
                    Debug.Assert((!oldId.IsEmpty) || _unnamedNodes[node.Address] == node);
                    Debug.Assert(newId.Equals(node.NodeId));

                    // Remove old id
                    if (!oldId.IsEmpty)
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
