
namespace Lucky.Home.Core
{
    public class ServiceBase : IService
    {
        protected ServiceBase(string logName)
        {
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
