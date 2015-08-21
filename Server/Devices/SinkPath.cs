using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices
{
    [DataContract]
    public class SinkPath
    {
        [DataMember]
        public Guid NodeId { get; set; }

        [DataMember]
        public string SinkId { get; set; }

        public SinkPath()
        { }

        public SinkPath(Guid nodeId, string sinkId)
        {
            NodeId = nodeId;
            SinkId = sinkId;
        }

        public override int GetHashCode()
        {
            return NodeId.GetHashCode() * 17171 + SinkId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return NodeId == ((SinkPath)obj).NodeId && SinkId.Equals(((SinkPath)obj).SinkId);
        }
    }
}