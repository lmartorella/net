using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Lucky.Home.Protocol;

namespace Lucky.Home.Admin
{
    [DataContract]
    public class Node
    {
        [DataMember]
        public Guid Id;

        [DataMember]
        public List<Node> Children = new List<Node>();

        public Node()
        { }

        internal Node(ITcpNode tcpNode)
        {
            TcpNode = tcpNode;
            Id = tcpNode.Id;
        }

        internal ITcpNode TcpNode { get; private set; }
    }
}