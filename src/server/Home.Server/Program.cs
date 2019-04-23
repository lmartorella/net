using Lucky.Services;
using Lucky.Home.Devices;
using Lucky.Home.Sinks;
using System.Reflection;
using System.Threading.Tasks;
using Lucky.Home.Application;
using Lucky.Home.Services;
using Lucky.Home.Protocol;
using System;
using Lucky.Home.Admin;

namespace Lucky.Home.Lib
{
    static class Program
    {
        public static void Main(string[] arguments)
        {
            Manager.Register<LoggerFactory, ILoggerFactory>();
            Manager.Register<ConfigurationService, IConfigurationService>();
            Manager.GetService<ConfigurationService>().Init(arguments);

            LoggerFactory.Init(Manager.GetService<PersistenceService>());

            Manager.GetService<IIsolatedStorageService>().InitAppRoot("Server");

            Manager.Register<Server, IServer>();
            Manager.Register<NodeManager, INodeManager>();
            Manager.Register<SinkManager>();
            Manager.Register<SinkManager, ISinkManager>();
            Manager.Register<DeviceManager, IDeviceManager>();
            Manager.GetService<ISinkManager>().RegisterType(typeof(SystemSink));

            LibraryLoad("app");

            Manager.GetService<DeviceManager>().Load();

            // Start server
            Manager.GetService<IServer>();

            // Start Admin connection
            Manager.GetService<AdminListener>();

            _ = Manager.GetService<AppService>().Start();

            Manager.GetService<SinkManager>().ResetSink += (o, e) =>
            {
                var systemSink = e.Sink.Node.Sink<ISystemSink>();
                if (systemSink != null)
                {
                    systemSink.Reset();
                }
            };

            Manager.GetService<AppService>().Run();
        }

        private static void LibraryLoad(string path)
        {
            throw new NotImplementedException();
            //// Register app service
            //Manager.Register<AppService>();

            //// Register app sinks
            //Manager.GetService<ISinkManager>().RegisterAssembly(Assembly.GetExecutingAssembly());
            //// Register devices
            //Manager.GetService<IDeviceManager>().RegisterAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
