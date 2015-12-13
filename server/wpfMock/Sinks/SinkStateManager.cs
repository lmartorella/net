using System.Runtime.Serialization;
using Lucky.Home;
using Lucky.Services;

namespace Lucky.HomeMock.Sinks
{
    [DataContract]
    public class SinkState
    {
        [DataMember]
        public NodeStatus NodeStatus { get; set; }
    }

    class SinkStateManager : ServiceBaseWithData<SinkState>
    {
        public NodeStatus NodeStatus
        {
            get
            {
                var sinkState = State;
                if (sinkState != null)
                {
                    return sinkState.NodeStatus;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                var state = State ?? new SinkState();
                state.NodeStatus = value;
                State = state;
            }
        }
    }
}
