using System;
using Lucky.Home.Core;

namespace Lucky.Home.Protocol
{
    interface INodeRegistrar : IService
    {
        void RegisterNode(Guid guid, TcpNodeAddress address);
        void HeartbeatNode(Guid guid, TcpNodeAddress address);
        void RefetchSubNodes(Guid guid, TcpNodeAddress address);
        ITcpNode FindNode(Guid guid);
    }
}
