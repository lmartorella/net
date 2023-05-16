using System;

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

            /// <summary>
            /// SubSink index
            /// </summary>
            public int SubIndex;
        }

        /// <summary>
        /// Event raised when one sub changed (with timestamp)
        /// </summary>
        public event EventHandler<EventReceivedEventArgs> EventReceived;
    }
}
