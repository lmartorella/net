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
                Manager.Register<ConfigurationService, IConfigurationService>();
                Manager.GetService<ConfigurationService>().Init(arguments);

                // Register app sinks
                Manager.GetService<ISinkManager>().RegisterAssembly(Assembly.GetExecutingAssembly());
                // Register devices
                Manager.GetService<IDeviceManager>().RegisterAssembly(Assembly.GetExecutingAssembly());
            });

            Manager.GetService<HomeApp>().Run();
        }
    }
}
