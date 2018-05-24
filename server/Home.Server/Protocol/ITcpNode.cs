using System;
using System.Threading.Tasks;
using Lucky.Home.Sinks;
using System.Runtime.CompilerServices;

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
        NodeId NodeId { get; }

        /// <summary>
        /// If some active connection action was previously failed, and not yet restored by a heartbeat
        /// </summary>
        bool IsZombie { get; set; }

        /// <summary>
        /// Get all available sinks
        /// </summary>
        ISink[] Sinks { get; }

        /// <summary>
        /// An already logged-in node relogs in (e.g. after node reset) or children changes
        /// </summary>
        Task Relogin(TcpNodeAddress address, int[] childrenChanged = null);

        /// <summary>
        /// Open sink communication for write
        /// </summary>
        Task<bool> WriteToSink(int sinkId, Func<IConnectionWriter, Task> writeHandler, [CallerMemberName] string context = "");

        /// <summary>
        /// Open sink communication for read
        /// </summary>
        /// <returns>True if communication succeded</returns>
        Task<bool> ReadFromSink(int sinkId, Func<IConnectionReader, Task> readHandler, [CallerMemberName] string context = "");

        /// <summary>
        /// Get the sink with the given type
        /// </summary>
        T Sink<T>() where T : ISink;

        /// <summary>
        /// Change the ID of the node
        /// </summary>
        Task<bool> Rename(NodeId newId);
    }
}