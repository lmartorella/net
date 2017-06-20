namespace Lucky.Services
{
    public interface IConfigurationService : IService
    {
        string GetConfig(string key);
    }
}
