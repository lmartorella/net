using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    interface INodeManager : IService
    {
        Task<ITcpNode> RegisterNode(Guid guid, TcpNodeAddress address);
        Task HeartbeatNode(Guid guid, TcpNodeAddress address);
        Task RefetchSubNodes(Guid guid, TcpNodeAddress address, int[] childrenChanged);
        // Register a unknown (no GUID) node, typically subnode, if not registered yet
        Task<ITcpNode> RegisterUnknownNode(TcpNodeAddress address);

        ITcpNode FindNode(Guid guid);
        ITcpNode FindNode(TcpNodeAddress address);
        IEnumerable<ITcpNode> Nodes { get; }

        void BeginRenameNode(TcpNode node, Guid newId);
        void EndRenameNode(TcpNode node, Guid oldId, Guid newId, bool success);
    }
}
