using System;

namespace Lucky.Home.Core
{
    public interface ILogger
    {
        void LogFormat(string type, string message, params object[] args);
    }

    public static class LoggerExtensions
    {
        private const string WARN = "WARN";
        private const string INFO = "info";
        private const string EXC = "EXC ";

        public static void Log(this ILogger logger, string message)
        {
            logger.LogFormat(INFO, message);
        }

        public static void Log(this ILogger logger, string message, string param1, object value1)
        {
            logger.LogFormat(INFO, "{0} [{1}]: {2}", message, param1, value1);
        }

        public static void Log(this ILogger logger, string message, string param1, object value1, string param2, object value2)
        {
            logger.LogFormat(INFO, "{0} [{1}]: {2} [{3}]: {4}", message, param1, value1, param2, value2);
        }

        public static void Warning(this ILogger logger, string message)
        {
            logger.LogFormat(WARN, message);
        }

        public static void Warning(this ILogger logger, string message, string param1, object value1)
        {
            logger.LogFormat(WARN, "{0} [{1}]: {2}", message, param1, value1);
        }

        public static void Warning(this ILogger logger, string message, string param1, object value1, string param2, object value2)
        {
            logger.LogFormat(WARN, "{0} [{1}]: {2} [{3}]: {4}", message, param1, value1, param2, value2);
        }

        public static void Exception(this ILogger logger, Exception exc)
        {
            logger.LogFormat(EXC, "{0}: Stack: {1}", exc.Message, UnrollStack(exc));
        }

        private static string UnrollStack(Exception exc)
        {
            return exc.StackTrace + ((exc.InnerException != null) ? (Environment.NewLine + "Inner exc: " + UnrollStack(exc.InnerException)) : String.Empty);
        }
    }
}
