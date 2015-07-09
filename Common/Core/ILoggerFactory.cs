namespace Lucky.Home.Core
{
    public interface ILoggerFactory : IService
    {
        ILogger Create(string name);
    }
}
