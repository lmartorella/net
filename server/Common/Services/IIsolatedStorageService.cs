﻿namespace Lucky.Services
{
    public interface IIsolatedStorageService : IService
    {
        void InitAppRoot(string appRoot);
        T GetState<T>(string serviceName) where T : class, new();
        void SetState<T>(string serviceName, T value) where T : class;
    }
}