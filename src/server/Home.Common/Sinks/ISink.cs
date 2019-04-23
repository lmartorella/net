﻿using Lucky.Home.Devices;
using Lucky.Home.Protocol;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Interface of a sink
    /// </summary>
    public interface ISink
    {
        string FourCc { get; }
        int SubCount { get; }
        bool IsOnline { get; }
    }
}