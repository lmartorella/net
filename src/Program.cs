using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Lucky.Home.Application;
using Lucky.Home.Core;
using Lucky.Home.Protocol;

[assembly: InternalsVisibleTo("SpeechTests")]

namespace Lucky.Home
{
    static class Program
    {
        static void Main()
        {
            Manager.Register<ConsoleLoggerFactory, ILoggerFactory>();
            Manager.GetService<IPersistenceService>().InitAppRoot("Server");

            Manager.Register<Server, IServer>();
            Manager.Register<NodeRegistrar, INodeRegistrar>();
            Manager.Register<SinkManager>();
            Manager.Register<AppService>();

            // Register known sinks
            Manager.Register<SinkManager>();
            Manager.GetService<SinkManager>().RegisterAssembly(Assembly.GetExecutingAssembly());

            // Start server
            Manager.GetService<IServer>();
            Console.Write("Server started. Ctrl+C to quit.");

            // Start app
            Manager.GetService<AppService>().Run();
        }
    }
}
