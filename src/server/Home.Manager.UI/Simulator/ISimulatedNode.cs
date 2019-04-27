using Lucky.Home.Protocol;
using Lucky.Home.Services;
using System;

namespace Lucky.Home.Simulator
{
    /// <summary>
    /// Base interface for simulated nodes (master or slave)
    /// </summary>
    public interface ISimulatedNode
    {
        /// <summary>
        /// Get the logger
        /// </summary>
        ILogger Logger { get; }

        /// <summary>
        /// Get the status serializer
        /// </summary>
        IStateProvider StateProvider { get; }
    }

    public interface IStateProvider
    {
        /// <summary>
        /// Get/set the node ID
        /// </summary>
        Guid Id { get; set; }
    }

    internal interface IStateProviderInternal : IStateProvider
    {
        /// <summary>
        /// Get/set the serializable state
        /// </summary>
        NodeStatus Status { get; set; }
    }
}
