using System;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Passive/poll based digital input array
    /// </summary>
    internal interface IDigitalInputArraySink : ISink
    {
        /// <summary>
        /// Get the current switch status
        /// </summary>
        bool[] Status { get; }

        /// <summary>
        /// Get/set the poll period. By default 1 sec.
        /// </summary>
        TimeSpan PollPeriod { get; set; }

        /// <summary>
        /// Event raised when a switch status changes
        /// </summary>
        event EventHandler StatusChanged;
    }
}