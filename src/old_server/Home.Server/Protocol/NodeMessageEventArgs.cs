using System;

namespace Lucky.Home.Protocol
{
    class NodeMessageEventArgs : EventArgs
    {
        public NodeId NodeId { get; set; }
        public TcpNodeAddress Address { get; set; }
        public PingMessageType MessageType { get; set; }

        /// <summary>
        /// Children changed indexes, +1 base
        /// </summary>
        public int[] ChildrenChanged { get; set; }

        /// <summary>
        /// Children changed indexes, +1 base
        /// </summary>
        public int[] AliveChildren { get; set; }
    }
}
