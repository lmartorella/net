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
        bool IsFullOnline { get; }

        /// <summary>
        /// Raised when the <see cref="IsFullOnline"/> changes
        /// </summary>
        event EventHandler IsFullOnlineChanged;
    }
}