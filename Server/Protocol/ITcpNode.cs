using System;
using System.Threading.Tasks;
using Lucky.Home.Core;

namespace Lucky.Home.Protocol
{
    internal interface ITcpNode
    {
        /// <summary>
        /// Current address
        /// </summary>
        TcpNodeAddress Address { get; }

        /// <summary>
        /// The Unique ID of the node, cannot be empty 
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Manage heartbeat
        /// </summary>
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
        Task WriteToSink(int sinkId, Action<IConnectionWriter> writeHandler);

        /// <summary>
        /// Open sink communication for read
        /// </summary>
        /// <returns>True if communication succeded</returns>
        Task ReadFromSink(int sinkId, Action<IConnectionReader> readHandler);

        /// <summary>
        /// Get the sink with the given type
        /// </summary>
        T Sink<T>() where T : ISink;
    }
}