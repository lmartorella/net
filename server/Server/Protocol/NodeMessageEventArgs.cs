using System;

namespace Lucky.Home.Protocol
{
    class NodeMessageEventArgs : EventArgs
    {
        public Guid Guid { get; private set; }
        public TcpNodeAddress Address { get; private set; }
        public PingMessageType MessageType { get; private set; }
        public int[] ChildrenChanged { get; private set; }

        public NodeMessageEventArgs(Guid guid, TcpNodeAddress address, PingMessageType messageType, int[] childrenChanged)
        {
            MessageType = messageType;
            Guid = guid;
            Address = address;
            ChildrenChanged = childrenChanged;
        }
    }
}
