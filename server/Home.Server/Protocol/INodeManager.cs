using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    interface INodeManager : IService
    {
        Task<ITcpNode> RegisterNode(NodeId guid, TcpNodeAddress address);
        Task HeartbeatNode(NodeId guid, TcpNodeAddress address);
        Task RefetchSubNodes(NodeId guid, TcpNodeAddress address, int[] childrenChanged);
        // Register a unknown (no GUID) node, typically subnode, if not registered yet
        Task<ITcpNode> RegisterUnknownNode(TcpNodeAddress address);

        ITcpNode FindNode(NodeId guid);
        ITcpNode FindNode(TcpNodeAddress address);
        IEnumerable<ITcpNode> Nodes { get; }

        void BeginRenameNode(TcpNode node, NodeId newId);
        void EndRenameNode(TcpNode node, NodeId oldId, NodeId newId, bool success);
    }
}
