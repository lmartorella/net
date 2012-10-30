using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
