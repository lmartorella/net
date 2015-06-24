using System;

namespace Lucky.Home.Core
{
    interface INodeRegistrar : IService
    {
        void RegisterNode(Guid guid, TcpNodeAddress address);
        void HeartbeatNode(Guid guid, TcpNodeAddress address);
        void RefetchSubNodes(Guid guid, TcpNodeAddress address);
    }
}
