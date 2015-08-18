﻿using System.Runtime.Serialization;

namespace Lucky.Home
{
    [DataContract]
    public class NodeStatus
    {
        [DataMember]
        public ResetReason ResetReason { get; set; }

        [DataMember]
        public string ExceptionMessage { get; set; }        
    }
}