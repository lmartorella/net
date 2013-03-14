using System;

namespace Lucky.Home.Core
{
    class Logger : ILogger
    {
        public void Log(string message)
        {
            LogFormat(message);
        }

        public void Log(string message, string param1, object value1)
        {
            LogFormat("{0} [{1}]: {2}", message, param1, value1);
        }

        public void Log(string message, string param1, object value1, string param2, object value2)
        {
            LogFormat("{0} [{1}]: {2} [{3}]: {4}", message, param1, value1, param2, value2);
        }

        private void LogFormat(string message, params object[] args)
        {
            Console.WriteLine(message, args);
        }
    }
}
