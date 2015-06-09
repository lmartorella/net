using System;

namespace Lucky.Home.Core
{
    internal class ConsoleLogger : ILogger
    {
        private readonly string _name;
        private const string WARN = "WARNING";
        private const string INFO = "info   ";

        public ConsoleLogger(string name)
        {
            _name = name;
        }

        public void Dispose()
        {
        }

        public void Log(string message)
        {
            LogFormat(INFO, message);
        }

        public void Log(string message, string param1, object value1)
        {
            LogFormat(INFO, "{0} [{1}]: {2}", message, param1, value1);
        }

        public void Log(string message, string param1, object value1, string param2, object value2)
        {
            LogFormat(INFO, "{0} [{1}]: {2} [{3}]: {4}", message, param1, value1, param2, value2);
        }

        public void Warning(string message)
        {
            LogFormat(WARN, message);
        }

        public void Warning(string message, string param1, object value1)
        {
            LogFormat(WARN, "{0} [{1}]: {2}", message, param1, value1);
        }

        public void Warning(string message, string param1, object value1, string param2, object value2)
        {
            LogFormat(WARN, "{0} [{1}]: {2} [{3}]: {4}", message, param1, value1, param2, value2);
        }

        private void LogFormat(string type, string message, params object[] args)
        {
            Console.WriteLine(type + "|" + _name + ": " + message, args);
        }
    }
}
