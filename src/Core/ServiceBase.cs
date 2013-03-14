using System;

namespace Lucky.Home.Core
{
    class ServiceBase : IService
    {
        protected ILogger Logger
        {
            get
            {
                return Manager.GetService<ILogger>();
            }
        }
    }
}
