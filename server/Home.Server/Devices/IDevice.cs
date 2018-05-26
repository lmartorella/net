using System;

namespace Lucky.Home.Devices
{
    public interface IDevice
    {
        /// <summary>
        /// Get the sink paths of involved sinks
        /// </summary>
        SinkPath[] SinkPaths { get; }

        /// <summary>
        /// Are all the relevant sinks connected?
        /// </summary>
        bool IsFullOnline { get; }

        /// <summary>
        /// Raised when the <see cref="IsFullOnline"/> changes
        /// </summary>
        event EventHandler IsFullOnlineChanged;
    }
}