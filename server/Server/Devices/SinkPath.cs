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

        /// <summary>
        /// Optional, sub-index inside the sink
        /// </summary>
        [DataMember]
        public int SubIndex { get; set; }

        public SinkPath()
        { }

        public SinkPath(Guid nodeId, string sinkId, int subIndex = -1)
        {
            NodeId = nodeId;
            SinkId = sinkId;
            SubIndex = subIndex;
        }

        public override int GetHashCode()
        {
            return NodeId.GetHashCode() * 17171 + SinkId.GetHashCode() + 7 * SubIndex;
        }

        public override bool Equals(object obj)
        {
            return NodeId == ((SinkPath)obj).NodeId && SinkId.Equals(((SinkPath)obj).SinkId) && SubIndex == ((SinkPath)obj).SubIndex;
        }
    }
}