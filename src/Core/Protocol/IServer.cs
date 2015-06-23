using System;
using System.Net;

namespace Lucky.Home.Core.Protocol
{
    interface IServer : IService
    {
        /// <summary>
        /// Get the public host addresses
        /// </summary>
        IPAddress[] Addresses { get; }
    }
}
