using System.Net;
using Lucky.Services;

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
