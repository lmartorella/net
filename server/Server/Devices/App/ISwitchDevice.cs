using System;

namespace Lucky.Home.Devices.App
{
    public interface ISwitchDevice : IDevice
    {
        bool Status { get; }
        event EventHandler StatusChanged;
    }
}