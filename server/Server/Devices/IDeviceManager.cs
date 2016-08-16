using Lucky.Services;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Lucky.Home.Devices
{
    public interface IDeviceManager : IService
    {
        void RegisterAssembly(Assembly assembly);

        object DevicesLock { get; }
        IEnumerable<IDevice> Devices { get; }
        event EventHandler DevicesChanged;
    }
}
