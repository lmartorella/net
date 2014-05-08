using System;
using System.Net;

namespace Lucky.Home.Core
{
    interface IServer : IService
    {
        /// <summary>
        /// Get the public host address
        /// </summary>
        IPAddress[] Addresses { get; }

        /// <summary>
        /// Get the public host service port (TCP)
        /// </summary>
        ushort Port { get; }
    }
}
