using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Lucky.Home.Admin;
using Lucky.Home.Application;
using Lucky.Home.Devices;
using Lucky.Home.Protocol;
using Lucky.Home.Sinks;
using Lucky.Services;

[assembly: InternalsVisibleTo("SpeechTests")]

namespace Lucky.Home
{
    static class Program
    {
        static void Main()
        {
            Manager.Register<LoggerFactory, ILoggerFactory>();
            Manager.GetService<IPersistenceService>().InitAppRoot("Server");

            Manager.Register<Server, IServer>();
            Manager.Register<NodeManager, INodeManager>();
            Manager.Register<SinkManager>();
            Manager.Register<AppService>();

            // Register known sinks
            Manager.Register<SinkManager>();
            Manager.GetService<SinkManager>().RegisterAssembly(Assembly.GetExecutingAssembly());

            // Register devices
            Manager.Register<DeviceManager>();
            Manager.GetService<DeviceManager>().RegisterAssembly(Assembly.GetExecutingAssembly());

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
