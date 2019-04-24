﻿namespace Lucky.Services
{
    /// <summary>
    /// Access CLI config switches
    /// </summary>
    public interface IConfigurationService : IService
    {
        string GetConfig(string key);
    }
}
