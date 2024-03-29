﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Home.Protocol;
using Lucky.Home.Sinks;
using Lucky.Home.Services;

namespace Lucky.Home.Admin
{
    /// <summary>
    /// Service to serve admin data
    /// </summary>
    class AdminServer : IAdminInterface
    {
        private readonly NodeManager _manager;

        public AdminServer()
        {
            _manager = Manager.GetService<NodeManager>();
        }

        public Task<Node[]> GetTopology()
        {
            return Task.FromResult(BuildTree());
        }

        public Task<bool> RenameNode(string nodeAddress, NodeId oldId, NodeId newId)
        {
            ITcpNode node;
            if (oldId.IsEmpty)
            {
                node = _manager.FindNodeByAddress(TcpNodeAddress.Parse(nodeAddress));
            }
            else
            {
                node = _manager.FindNode(oldId);
            }
            if (node != null)
            {
                node.Rename(newId);
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public async Task ResetNode(NodeId id, string nodeAddress)
        {
            ITcpNode node;
            if (!id.IsEmpty)
            {
                node = _manager.FindNode(id);
            }
            else
            {
                node = _manager.FindNodeByAddress(TcpNodeAddress.Parse(nodeAddress));
            }

            if (node != null)
            {
                await node.Sink<SystemSink>().Reset();
            }
        }

        private Node BuildNode(ITcpNode tcpNode)
        {
            var systemSink = tcpNode.Sink<ISystemSink>();
            return new Node()
            {
                NodeId = tcpNode.NodeId,
                Status = systemSink != null ? systemSink.Status : null,
                Address = tcpNode.Address.ToString(),
                Sinks = tcpNode.Sinks.Select(s => s.FourCc).ToArray(),
                SubSinkCount = tcpNode.Sinks.Select(s => s.SubCount).ToArray(),
                IsZombie = tcpNode.IsZombie
            };
        }

        private Node[] BuildTree()
        {
            var roots = _manager.Nodes.Where(n => !n.Address.IsSubNode).Select(BuildNode).ToList();
            foreach (var node in _manager.Nodes.Where(n => n.Address.IsSubNode))
            {
                var root = roots.FirstOrDefault(r => r.Address == node.Address.SubNode(0).ToString());
                if (root != null)
                {
                    root.Children = root.Children.Concat(new[] { BuildNode(node) }).ToArray();
                }
            }
            return roots.ToArray();
        }
    }
}
