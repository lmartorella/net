
namespace Lucky.Home.Core
{
    public class ServiceBase : IService
    {
        protected readonly string LogName;

        protected ServiceBase(string logName)
        {
            LogName = logName;
            if (logName != null)
            {
                Logger = Manager.GetService<ILoggerFactory>().Create(logName);
            }
        }

        protected ILogger Logger { get; private set; }

        public virtual void Dispose()
        { }
    }
}
