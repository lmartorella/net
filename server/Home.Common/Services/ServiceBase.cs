using System.Linq;

namespace Lucky.Services
{
    public class ServiceBase : IService
    {
        protected readonly string LogName;

        protected ServiceBase(bool verboseLog = false)
        {
            if (!GetType().GetInterfaces().Any(t => t == typeof(ILoggerFactory)))
            {
                LogName = GetType().Name;
                if (LogName.EndsWith("Service"))
                {
                    LogName = LogName.Substring(0, LogName.Length - "Service".Length);
                }
                Logger = Manager.GetService<ILoggerFactory>().Create(LogName, verboseLog);
            }
        }

        public ILogger Logger { get; private set; }

        public virtual void Dispose()
        { }
    }
}
