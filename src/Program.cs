using System;
using System.Reflection;
using Lucky.Home.Core;

namespace Lucky.Home
{
    class Program
    {
        static void Main()
        {
            Manager.Register<HelloListener, IHelloListener>();
            Manager.Register<Logger, ILogger>();
            Manager.Register<Server, IServer>();
            Manager.Register<SinkManager, SinkManager>();

            Manager.GetService<SinkManager>().RegisterAssembly(Assembly.GetExecutingAssembly());

            // Start server
            Manager.GetService<IServer>();

            // Wait for Ctrl+Break
            Console.WriteLine("Press CTRL+C to exit....");

            while (true)
            {
                Console.Read();
            }
        }
    }
}
