using System;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Interface of a device objet
    /// </summary>
    public interface IDevice
    {
        /// <summary>
        /// Are all the relevant sinks connected?
        /// </summary>
        OnlineStatus OnlineStatus { get; }

        /// <summary>
        /// Raised when the <see cref="OnlineStatus"/> changes
        /// </summary>
        event EventHandler OnlineStatusChanged;
    }
}