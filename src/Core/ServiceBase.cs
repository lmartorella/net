
namespace Lucky.Home.Core
{
    class ServiceBase : IService
    {
        public ServiceBase(string logName)
        {
            Logger = new ConsoleLogger(logName);
        }

        protected ILogger Logger { get; private set; }

        public virtual void Dispose()
        { }
    }
}
