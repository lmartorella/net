using System;
using Lucky.Services;

namespace Lucky.Home.Application
{
    class ConfigurationService : ServiceBase, IConfigurationService
    {
        private string[] _arguments;

        internal void Init(string[] arguments)
        {
            _arguments = arguments;
        }

        public string GetConfig(string key)
        {
            for (int i = 0; i < _arguments.Length - 1; i++)
            {
                if (_arguments[i] == "-" + key)
                {
                    return _arguments[i + 1];
                }
            }
            return null;
        }
    }
}
