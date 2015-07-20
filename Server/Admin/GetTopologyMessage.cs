using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    /// <summary>
    /// Get node topology as connection tree
    /// </summary>
    [DataContract]
    public class GetTopologyMessage : AdminMessage
    {
        [DataContract]
        public class Response
        {
            public Node[] Roots;
        }

        [DataContract]
        public class Node
        {
            public Guid Id;
            public Node[] Children;
        }
    }
}
