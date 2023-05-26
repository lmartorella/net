using Lucky.Home.Services;
using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices.Garden
{
    class DigitalInputArrayRpc : BaseRpc
    {
        /// <summary>
        /// A single input (sub-sink) changed in a moment in time (past)
        /// </summary>
        public class EventReceivedEventArgs : EventArgs
        {
            /// <summary>
            /// Event timestamp
            /// </summary>
            public DateTime Timestamp;

            /// <summary>
            /// Single switch state (sub-sink)
            /// </summary>
            public bool State;
        }

        [DataContract]
        public class StateValue
        {
            /// <summary>
            /// Event timestamp
            /// </summary>
            [DataMember(Name = "timestamp")]
            public string Timestamp;

            /// <summary>
            /// Single switch state (sub-sink)
            /// </summary>
            [DataMember(Name = "state")]
            public bool? State;

            [DataMember(Name = "offline")]
            public bool Offline;
        }

        public DigitalInputArrayRpc()
        {
            _ = mqttService.SubscribeJsonTopic<StateValue>("pump_switch_0/value", state =>
            {
                if (state.Offline)
                {
                    IsOnline = false;
                } 
                else
                {
                    IsOnline = true;
                    if (state.State.HasValue && state.Timestamp != null)
                    {
                        EventReceived?.Invoke(this, new EventReceivedEventArgs { State = state.State.Value, Timestamp = DateTime.Parse(state.Timestamp) });
                    }
                }
            });
        }

        /// <summary>
        /// Event raised when one sub changed (with timestamp)
        /// </summary>
        public event EventHandler<EventReceivedEventArgs> EventReceived;
    }
}
