using System;
using System.Linq;
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
        public string Address { get; set; } 

        [DataMember]
        public Node[] Children { get; set; }

        [DataMember]
        public NodeStatus Status { get; set; }

        [DataMember]
        public string[] Sinks{ get; set; }

        [DataMember]
        public int[] SubSinkCount { get; set; }

        [DataMember]
        public bool IsZombie{ get; set; }

        public Node()
        {
            Children = new Node[0];
        }

        internal Node(ITcpNode tcpNode)
            :this()
        {
            TcpNode = tcpNode;
            Id = tcpNode.Id;
            var systemSink = tcpNode.Sink<ISystemSink>();
            Status = systemSink != null ? systemSink.Status : null;
            Address = tcpNode.Address.ToString();
            Sinks = tcpNode.Sinks.Select(s => s.FourCc).ToArray();
            SubSinkCount = tcpNode.Sinks.Select(s => s.SubCount).ToArray();
            IsZombie = tcpNode.IsZombie;
        }

        internal ITcpNode TcpNode { get; private set; }
    }
}