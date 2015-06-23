using System;
using System.Net;
using System.Threading.Tasks;

namespace Lucky.Home.Core.Protocol
{
    internal interface INode
    {
        /// <summary>
        /// The Unique ID of the node, cannot be empty 
        /// </summary>
        Guid Id { get; }

        void Heartbeat(IPAddress address);

        /// <summary>
        /// An already logged-in node relogs in (e.g. after node reset)
        /// </summary>
        Task Relogin(IPAddress address);
    }
}