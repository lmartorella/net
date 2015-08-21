using System;

namespace Lucky.Home.Devices
{
    public interface IDevice : IDisposable
    {
        /// <summary>
        /// Is the relevant sink/s connected?
        /// </summary>
        bool IsOnline { get; }

        /// <summary>
        /// Get the sink path
        /// </summary>
        SinkPath SinkPath { get; }
    }
}