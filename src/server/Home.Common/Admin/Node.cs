﻿using Lucky.Home.Protocol;
using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    /// <summary>
    /// Node object for admin API
    /// </summary>
    [DataContract]
    internal class Node
    {
        [DataMember]
        public NodeId NodeId { get; set; }

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
    }
}