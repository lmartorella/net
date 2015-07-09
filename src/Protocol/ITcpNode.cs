using System;
using System.IO;
using System.Threading.Tasks;

namespace Lucky.Home.Protocol
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

        /// <summary>
        /// Open sink communication for write
        /// </summary>
        bool WriteToSink(byte[] data, int sinkId);
    }
}