using System;

namespace Lucky.Home.Protocol
{
    class NodeMessageEventArgs : EventArgs
    {
        public NodeId NodeId { get; private set; }
        public TcpNodeAddress Address { get; private set; }
        public PingMessageType MessageType { get; private set; }
        public int[] ChildrenChanged { get; private set; }

        public NodeMessageEventArgs(NodeId nodeId, TcpNodeAddress address, PingMessageType messageType, int[] childrenChanged)
        {
            MessageType = messageType;
            NodeId = nodeId;
            Address = address;
            ChildrenChanged = childrenChanged;
        }
    }
}
