using System;
using Lucky.Services;

namespace Lucky
{
    /// <summary>
    /// Only console supported at the moment
    /// </summary>
    public class LoggerFactory : ServiceBase, ILoggerFactory
    {        
        public class ConsoleLogger : ILogger
        {
            private readonly string _name;

            public ConsoleLogger(string name)
            {
                _name = name;
            }

            public void LogFormat(string type, string message, params object[] args)
            {
                var ts = DateTime.Now.ToString("HH:mm:ss");
                Console.WriteLine(ts + " " + type + "|" + _name + ": " + message, args);
            }
        }

        /// <summary>
        /// Verbose not supported yet
        /// </summary>
        public ILogger Create(string name, bool verbose = false)
        {
            return new ConsoleLogger(name);
        }
    }
}
