using System.Collections.Generic;
using System.Runtime.Serialization;
using Lucky.Home.Protocol;

namespace Lucky.Home.Admin
{
    [DataContract]
    public class Node
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public List<Node> Children { get; set; }

        public Node()
        {
            Children = new List<Node>();
        }

        internal Node(ITcpNode tcpNode)
            :this()
        {
            TcpNode = tcpNode;
            Id = tcpNode.Id.ToString();
        }

        internal ITcpNode TcpNode { get; private set; }
    }
}