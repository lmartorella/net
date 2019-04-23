using Lucky.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Lucky.Home.Devices
{
    public interface IDeviceManager : IService
    {
        object DevicesLock { get; }
        IEnumerable<IDevice> Devices { get; }
        event EventHandler DevicesChanged;
    }

    internal interface IDeviceManagerInternal : IDeviceManager
    {
        void RegisterAssembly(Assembly assembly);
        Task TerminateAll();
    }
}
