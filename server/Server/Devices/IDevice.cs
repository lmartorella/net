using System;

namespace Lucky.Home.Devices
{
    public interface IDevice : IDisposable
    {
        /// <summary>
        /// Get the sink path
        /// </summary>
        SinkPath SinkPath { get; }

        /// <summary>
        /// Instance argument
        /// </summary>
        string Argument { get; }

        /// <summary>
        /// Is the relevant sink/s connected?
        /// </summary>
        bool IsOnline { get; }
    }
}