using System;
using Lucky.Home.Admin;
using Lucky.Home.Application;
using Lucky.Home.Devices;
using Lucky.Home.Protocol;
using Lucky.Home.Sinks;
using Lucky.Services;

namespace Lucky.Home
{
    public static class ServerEntryPoint
    {
        public static void Load(Action registerHandler)
        {
            Manager.Register<LoggerFactory, ILoggerFactory>();
            Manager.GetService<IPersistenceService>().InitAppRoot("Server");

            Manager.Register<Server, IServer>();
            Manager.Register<NodeManager, INodeManager>();
            Manager.Register<SinkManager>();
            Manager.Register<AppService>();
            Manager.Register<SinkManager, ISinkManager>();
            Manager.Register<DeviceManager, IDeviceManager>();
            Manager.GetService<ISinkManager>().RegisterType(typeof(SystemSink));

            registerHandler();

            Manager.GetService<DeviceManager>().Load();

            // Start server
            Manager.GetService<IServer>();
            Console.WriteLine("Server started. Ctrl+C to quit.");

            // Start Admin connection
            Manager.GetService<AdminListener>();

            // Start app
            Manager.GetService<AppService>().Run();
        }
    }
}
