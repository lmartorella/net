using System.Runtime.Serialization;
using Lucky.Home.Protocol;
using Lucky.Home.Services;

namespace Lucky.HomeMock.Sinks
{
    [DataContract]
    internal class SinkState
    {
        [DataMember]
        public NodeStatus NodeStatus { get; set; }
        [DataMember]
        public NodeStatus ChildNodeStatus { get; set; }
    }

    class SinkStateManager : ServiceBaseWithData<SinkState>
    {
        public NodeStatus GetNodeStatus(bool child)
        {
            var sinkState = State;
            if (sinkState != null)
            {
                return child ? sinkState.ChildNodeStatus : sinkState.NodeStatus;
            }
            else
            {
                return null;
            }
        }

        public void SetNodeStatus(NodeStatus value, bool child)
        { 
            var state = State ?? new SinkState();
            if (child)
            {
                state.ChildNodeStatus = value;
            }
            else
            {
                state.NodeStatus = value;
            }
            State = state;
        }
    }
}
