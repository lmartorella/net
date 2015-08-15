namespace Lucky.Services
{
    public interface ILoggerFactory : IService
    {
        ILogger Create(string name);
    }
}
