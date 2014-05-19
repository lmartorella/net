using System;
using System.Reflection;
using Lucky.Home.Core;
using Lucky.Home.Sinks;

namespace Lucky.Home
{
    class Program
    {
        static void Main()
        {
            //Manager.Register<HelloListener, IHelloListener>();
            Manager.Register<Logger, ILogger>();
            Manager.Register<Server, IServer>();
            Manager.Register<SinkManager, SinkManager>();

            Manager.GetService<SinkManager>().RegisterAssembly(Assembly.GetExecutingAssembly());

            // Start server
            Manager.GetService<IServer>();

            while (true)
            {
                Console.Write("Enter 's' to start stream test or 'q' to quit: ");
                string str = Console.ReadLine();
                switch (str)
                {
                    case "q":
                        return;
                    case "s":
                        AudioPlayerSink.StartTest();
                        break;
                }
            }
        }
    }
}
