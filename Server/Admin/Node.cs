using System;
using System.Runtime.Serialization;
using Lucky.Home.Protocol;
using Lucky.Home.Sinks;

namespace Lucky.Home.Admin
{
    [DataContract]
    public class Node
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public Node[] Children { get; set; }

        [DataMember]
        public NodeStatus Status { get; set; }

        public Node()
        {
            Children = new Node[0];
        }

        internal Node(ITcpNode tcpNode)
            :this()
        {
            TcpNode = tcpNode;
            Id = tcpNode.Id;
            Status = tcpNode.Sink<ISystemSink>().Status;
        }

        internal ITcpNode TcpNode { get; private set; }
    }
}