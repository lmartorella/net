using System;

namespace Lucky.Home.Devices
{
    public interface ISwitchDevice : IDevice
    {
        bool Status { get; }
        event EventHandler StatusChanged;
    }
}