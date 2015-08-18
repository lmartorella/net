using System;

namespace Lucky.Home.Security.Sensors
{
    class DoorSwitch : ISensor
    {
        public string DisplayName { get; private set; }

        public SwitchStatus Status { get; private set; }

        public event EventHandler StatusChanged;
    }
}
