namespace Lucky.Services
{
    /// <summary>
    /// Service to create loggers
    /// </summary>
    public interface ILoggerFactory : IService
    {
        ILogger Create(string name, bool verbose = false);
    }
}
