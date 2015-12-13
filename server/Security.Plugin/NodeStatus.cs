namespace Lucky.Home.Security
{
    internal enum NodeStatus
    {
        /// <summary>
        /// Node uninitialized
        /// </summary>
        None,

        /// <summary>
        /// The node is offline. If sensor, it cannot be armed.
        /// When armed, if a node goes in offline mode usually means alarm.
        /// </summary>
        Offline,

        /// <summary>
        /// Normal state, no alarm.
        /// </summary>
        Normal,

        /// <summary>
        /// Pre-alarm state (unconfirmed alarm). 
        /// Two or more sensors in pre-alarm means alarm.
        /// Node in pre-alarm can be armed.
        /// </summary>
        PreAlarm,

        /// <summary>
        /// Confirmed alarm state.
        /// </summary>
        Alarm,
    }
}