using System;
using System.Collections.Generic;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    interface INodeManager : IService
    {
        void RegisterNode(Guid guid, TcpNodeAddress address);
        void HeartbeatNode(Guid guid, TcpNodeAddress address);
        void RefetchSubNodes(Guid guid, TcpNodeAddress address);
        void RegisterUnknownNode(TcpNodeAddress address);

        ITcpNode FindNode(Guid guid);
        ITcpNode FindNode(TcpNodeAddress address);
        IEnumerable<ITcpNode> Nodes { get; }

        void BeginRenameNode(TcpNode node, Guid newId);
        void EndRenameNode(TcpNode node, Guid oldId, Guid newId, bool success);
    }
}
