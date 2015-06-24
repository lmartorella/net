using System;
using System.Net;

namespace Lucky.Home.Core.Protocol
{
    class NodeMessageEventArgs : EventArgs
    {
        public Guid Guid { get; private set; }
        public TcpNodeAddress Address { get; private set; }
        public PingMessageType MessageType { get; private set; }

        public NodeMessageEventArgs(Guid guid, TcpNodeAddress address, PingMessageType messageType)
        {
            MessageType = messageType;
            Guid = guid;
            Address = address;
        }
    }
}
