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
        private readonly Dictionary<Guid, TcpNode> _nodes = new Dictionary<Guid, TcpNode>();
        private readonly Dictionary<TcpNodeAddress, TcpNode> _unnamedNodes = new Dictionary<TcpNodeAddress, TcpNode>();
        private readonly object _nodeLock = new object();

        /// <summary>
        /// A node is discovered through heartbeat
        /// </summary>
        public async Task<ITcpNode> RegisterNode(Guid guid, TcpNodeAddress address)
        {
            if (guid == Guid.Empty)
            {
                return await RegisterNewNode(address);
            }
            else
            {
                return await RegisterNamedNode(guid, address);
            }
        }

        private async Task<ITcpNode> RegisterNamedNode(Guid guid, TcpNodeAddress address)
        {
            TcpNode node;
            bool relogin;
            lock (_nodeLock)
            {
                node = FindById(guid);
                if (node == null)
                {
                    // New node!
                    node = new TcpNode(guid, address);
                    _nodes.Add(guid, node);
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
                    return newNode;
                }
                newNode = new TcpNode(Guid.Empty, address);
                _unnamedNodes.Add(address, newNode);
            }
            await newNode.FetchMetadata();
            return newNode;
        }

        public async Task<ITcpNode> RegisterUnknownNode(TcpNodeAddress address)
        {
            // Ask for guid
            var guid = new TcpNode(Guid.Empty, address).TryFetchGuid();
            if (!guid.HasValue)
            {
                // Error in fetching
                Logger.Warning("Error/timeout in RegisterUnknownNode of " + address);
                return null;
            }
            else
            {
                return await RegisterNode((Guid)guid, address);
            }
        }

        public async Task HeartbeatNode(Guid guid, TcpNodeAddress address)
        {
            TcpNode node;
            lock (_nodeLock)
            {
                node = guid != Guid.Empty ? FindById(guid) : FindUnnamed(address);
            }

            // Not known?
            if (node == null)
            {
                // The server was reset?
                await RegisterNode(guid, address);
            }
            else
            {
                // Normal heartbeat
                await node.Heartbeat(address);
            }
        }

        public async Task RefetchSubNodes(Guid guid, TcpNodeAddress address)
        {
            TcpNode node;
            lock (_nodeLock)
            {
                node = guid != Guid.Empty ? FindById(guid) : FindUnnamed(address);
            }

            // Not known?
            if (node == null)
            {
                // The server was reset?
                await RegisterNode(guid, address);
            }
            else
            {
                // Normal heartbeat
                await node.Relogin(address);
            }
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
