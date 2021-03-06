using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Serializable path to a sink
    /// </summary>
    [DataContract]
    internal class SinkPath
    {
        [DataMember]
        public NodeId NodeId { get; set; }

        [DataMember]
        public string SinkId { get; set; }

        /// <summary>
        /// Optional, sub-index inside the sink, negative if invalid
        /// </summary>
        [DataMember]
        public int SubIndex { get; set; }

        public SinkPath()
        { }

        public SinkPath(NodeId nodeId, string sinkId, int subIndex = -1)
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
            return NodeId.Equals(((SinkPath)obj).NodeId) && SinkId.Equals(((SinkPath)obj).SinkId) && SubIndex == ((SinkPath)obj).SubIndex;
        }

        public override string ToString()
        {
            return String.Format("{0}/{1}{2}", NodeId, SinkId, SubIndex >= 0 ? "." + SubIndex : "");
        }

        public bool Owns(SinkPath sinkPath)
        {
            return new SinkPath(sinkPath.NodeId, sinkPath.SinkId).Equals(this);
        }
    }
}