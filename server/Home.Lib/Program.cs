using Lucky.Services;
using Lucky.Home.Devices;
using Lucky.Home.Sinks;
using System.Reflection;

namespace Lucky.Home.Lib
{
    static class Program
    {
        public static void Main()
        {
            ServerEntryPoint.Load(() =>
            {
                // Register app sinks
                Manager.GetService<ISinkManager>().RegisterAssembly(Assembly.GetExecutingAssembly());
                // Register devices
                Manager.GetService<IDeviceManager>().RegisterAssembly(Assembly.GetExecutingAssembly());
            });
        }
    }
}
