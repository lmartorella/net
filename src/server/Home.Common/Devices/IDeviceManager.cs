using Lucky.Home.Services;
using System;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Manager for devices
    /// </summary>
    public interface IDeviceManager : IService
    {
        /// <summary>
        /// Get the list of the currently registered devices
        /// </summary>
        IDevice[] Devices { get; }

        /// <summary>
        /// Raised when the device list changes
        /// </summary>
        event EventHandler DevicesChanged;
    }
}
