using System;
using System.IO;
using Lucky.Home.Services;

namespace Lucky.Home
{
    /// <summary>
    /// Console logger
    /// </summary>
    internal class LoggerFactory : ServiceBase, ILoggerFactory
    {
        private static string s_logFile;

        public class ConsoleLogger : ILogger
        {
            private readonly string _name;
            public string SubKey { get; set; }

            public ConsoleLogger(string name, string subKey = null)
            {
                _name = name;
                SubKey = subKey;
            }

            public void LogFormat(string type, string message, params object[] args)
            {
                var ts = DateTime.Now.ToString("HH:mm:ss");
                var line = string.Format(ts + " " + type + "|" + _name + (SubKey != null ? "-" + SubKey : "") + ": " + message, args);
                Console.WriteLine(line);

                if (s_logFile != null)
                {
                    AppendLogFile(line);
                }
            }

            private void AppendLogFile(string line)
            {
                // Don't crash if another thread is locking the file
                lock (s_logFile)
                {
                    using (StreamWriter logger = new StreamWriter(s_logFile, true))
                    {
                        logger.WriteLine(line);
                    }
                }
            }
        }

        public static void Init(PersistenceService service)
        {
            s_logFile = Path.Combine(service.GetAppFolderPath(), "log.txt");
            Console.WriteLine("LOG: " + s_logFile);
        }

        /// <summary>
        /// Verbose not supported yet
        /// </summary>
        public ILogger Create(string name)
        {
            return new ConsoleLogger(name);
        }

        /// <summary>
        /// Verbose not supported yet
        /// </summary>
        public ILogger Create(string name, bool verbose)
        {
            return new ConsoleLogger(name);
        }

        /// <summary>
        /// Verbose not supported yet
        /// </summary>
        public ILogger Create(string name, string subKey)
        {
            return new ConsoleLogger(name, subKey);
        }
    }
}
