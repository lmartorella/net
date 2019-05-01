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
        /// Get/set the node ID
        /// </summary>
        Guid Id { get; set; }

        /// <summary>
        /// Event raised when id changes
        /// </summary>
        event EventHandler IdChanged;
    }

    internal interface ISimulatedNodeInternal : ISimulatedNode
    {
        /// <summary>
        /// Get/set the serializable state
        /// </summary>
        NodeStatus Status { get; set; }

        /// <summary>
        /// Simulate a reset
        /// </summary>
        Action Reset { get; set; }
    }
}
