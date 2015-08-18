using System;

namespace Lucky.Home.Security.Sensors
{
    internal interface ISensor
    {
        string DisplayName { get; }
        SwitchStatus Status { get; }
        event EventHandler StatusChanged;
    }
}