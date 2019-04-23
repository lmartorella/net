using Lucky.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

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
