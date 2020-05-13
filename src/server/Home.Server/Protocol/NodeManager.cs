using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Home.Services;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// Manager for nodes
    /// </summary>
    class NodeManager : ServiceBase
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
            Logger.Log("RegisterNamedNode", "nodeID", id, "address", address);
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
                    // Node already registered. Do a relogin/status fetch
                    relogin = true;
                }
            }

            if (relogin)
            {
                // The node was reset (Whatchdog?), and probably changed sub-index address
                await node.Relogin(address);
            }
            else
            {
                await node.FetchMetadata("registerNamed");
            }
            return node;
        }

        private async Task<ITcpNode> RegisterNewNode(TcpNodeAddress address)
        {
            Logger.Log("RegisterNewNode", "address", address);
            TcpNode newNode;
            lock (_nodeLock)
            {
                // Ignore consecutive messages
                if (_unnamedNodes.TryGetValue(address, out newNode))
                {
                    newNode.Dezombie("hello", address);
                }
                else
                {
                    newNode = new TcpNode(new NodeId(), address);
                    _unnamedNodes.Add(address, newNode);
                }
            }
            await newNode.FetchMetadata("registerNew");
            return newNode;
        }

        public async Task<ITcpNode> RegisterUnknownNode(TcpNodeAddress address, string context)
        {
            // Check if a node already exists for such address. In that case, use that instance to fetch GUID and 
            // then check if already registered
            var node = FindNodeByAddress(address) as TcpNode;
            var nodeCreated = false;
            if (node == null)
            {
                node = new TcpNode(new NodeId(), address);
                nodeCreated = true;
            }

            // Ask for guid
            var id = await node.TryFetchGuid();
            if (id == null)
            {
                // Error in fetching
                Logger.Warning("Error/timeout in RegisterUnknownNode of " + address);
                return null;
            }

            if (nodeCreated || !id.Equals(node.NodeId))
            {
                Logger.Log("RegisterUnknownNode", "address", address);
                return await RegisterNode(id, address);
            }
            else
            {
                // The node is exactly the same, simply re-fetch metadata
                await node.FetchMetadata("registerUnknown");
                node.Dezombie(context, address);
                return node;
            }
        }

        public async Task HeartbeatNode(NodeId id, TcpNodeAddress address, int[] aliveChildren)
        {
            TcpNode node;
            lock (_nodeLock)
            {
                node = (id.IsEmpty) ? FindUnnamed(address) : FindById(id);
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
                await node.Heartbeat(address, aliveChildren);
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

        public ITcpNode FindNode(NodeId id)
        {
            lock (_nodeLock)
            {
                return FindById(id);
            }
        }

        public ITcpNode FindNodeByAddress(TcpNodeAddress address)
        {
            var ret = FindUnnamed(address);
            if (ret == null)
            {
                ret = _nodes.Values.FirstOrDefault(n => n.Address.Equals(address));
            }
            return ret;
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
