using System;
using System.Threading.Tasks;

namespace Lucky.Home.Core
{
    internal interface ITcpNode
    {
        /// <summary>
        /// The Unique ID of the node, cannot be empty 
        /// </summary>
        Guid Id { get; }

        void Heartbeat(TcpNodeAddress address);

        /// <summary>
        /// An already logged-in node relogs in (e.g. after node reset)
        /// </summary>
        Task Relogin(TcpNodeAddress address);

        /// <summary>
        /// An already logged-in node changed its children (e.g. RS485 push-button)
        /// </summary>
        void RefetchChildren(TcpNodeAddress address);
    }
}