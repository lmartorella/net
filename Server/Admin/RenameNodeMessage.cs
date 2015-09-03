using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    [DataContract]
    public class RenameNodeMessage : AdminMessage
    {
        [DataMember]
        public Guid Id;

        [DataMember]
        public readonly string NodeAddress;

        [DataMember]
        public readonly Guid NewId;

        public RenameNodeMessage(Node node, Guid newId)
        {
            Id = node.Id;
            NodeAddress = node.Address;
            NewId = newId;
        }

        [DataContract]
        public class Response
        {
        }
    }
}