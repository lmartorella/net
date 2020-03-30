using System;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Access isolated storage
    /// </summary>
    internal interface IIsolatedStorageService : IService
    {
        void InitAppRoot(string appRoot);
        T GetState<T>(string serviceName, Func<T> defaultProvider);
        void SetState<T>(string serviceName, T value);
    }
}