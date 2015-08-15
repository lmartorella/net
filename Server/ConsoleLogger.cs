using System;
using Lucky.Services;

namespace Lucky.Home
{
    internal class ConsoleLoggerFactory : ServiceBase, ILoggerFactory
    {        
        private class ConsoleLogger : ILogger
        {
            private readonly string _name;

            public ConsoleLogger(string name)
            {
                _name = name;
            }

            public void LogFormat(string type, string message, params object[] args)
            {
                Console.WriteLine(type + "|" + _name + ": " + message, args);
            }
        }

        public ILogger Create(string name)
        {
            return new ConsoleLogger(name);
        }
    }
}
