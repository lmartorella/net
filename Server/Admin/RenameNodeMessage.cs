using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    [DataContract]
    public class RenameNodeMessage : AdminMessage
    {
        [DataMember]
        public readonly Guid Id;

        [DataMember]
        public readonly Guid NewId;

        public RenameNodeMessage(Guid id, Guid newId)
        {
            Id = id;
            NewId = newId;
        }

        [DataContract]
        public class Response
        {
        }
    }
}