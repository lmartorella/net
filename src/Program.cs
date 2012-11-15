﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Lucky.Home.Core;

namespace Lucky.Home
{
    class Program
    {
        static void Main(string[] args)
        {
            Manager.Register<HelloListener, IHelloListener>();
            Manager.Register<Logger, ILogger>();
            Manager.Register<Server, IServer>();
            Manager.Register<SinkManager, SinkManager>();

            // Start server
            Manager.GetService<IServer>();

            Manager.GetService<SinkManager>().RegisterAssembly(Assembly.GetExecutingAssembly());

            // Wait for Ctrl+Break
            Console.WriteLine("Press CTRL+C to exit....");

            while (true)
            {
                Console.Read();
            }
        }
    }
}
