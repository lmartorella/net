using System.Net;
using Lucky.Home.Core;

namespace Lucky.Home.Protocol
{
    interface IServer : IService
    {
        /// <summary>
        /// Get the public host addresses
        /// </summary>
        IPAddress[] Addresses { get; }
    }
}
