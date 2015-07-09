using System;

namespace Lucky.Home.Core
{
    internal class ConsoleLoggerFactory : ServiceBase, ILoggerFactory
    {
        public ConsoleLoggerFactory()
            : base(null)
        {
        }
        
        private class ConsoleLogger : ILogger
        {
            private readonly string _name;
            private const string WARN = "WARN";
            private const string INFO = "info";
            private const string EXC = "EXC ";

            public ConsoleLogger(string name)
            {
                _name = name;
            }

            void IDisposable.Dispose()
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

            public void Exception(Exception exc)
            {
                LogFormat(EXC, "{0}: Stack: {1}", exc.Message, UnrollStack(exc));
            }

            private string UnrollStack(Exception exc)
            {
                return exc.StackTrace + ((exc.InnerException != null) ? (Environment.NewLine + "Inner exc: " + UnrollStack(exc.InnerException)) : String.Empty);
            }

            private void LogFormat(string type, string message, params object[] args)
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
