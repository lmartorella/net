using Lucky.Services;
using Lucky.Home.Devices;
using Lucky.Home.Sinks;
using System.Reflection;
using Lucky.Home.Application;

namespace Lucky.Home.Lib
{
    static class Program
    {
        public static void Main(string[] arguments)
        {
            ServerEntryPoint.Load(() =>
            {
                // Register app sinks
                Manager.GetService<ISinkManager>().RegisterAssembly(Assembly.GetExecutingAssembly());
                // Register devices
                Manager.GetService<IDeviceManager>().RegisterAssembly(Assembly.GetExecutingAssembly());
            }, arguments);

            _ = Manager.GetService<HomeApp>().Start();
            Manager.GetService<HomeApp>().Run();
        }
    }
}
