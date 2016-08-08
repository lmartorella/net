using Lucky.Services;
using System.Reflection;

namespace Lucky.Home.Devices
{
    public interface IDeviceManager : IService
    {
        void RegisterAssembly(Assembly assembly);
    }
}
