using System.Collections.Generic;
using Lucky.Home.Devices;

namespace Lucky.Home.Security.Sensors
{
    class Window : SensorBase
    {
        private IEnumerable<IDevice> _devices;

        public Window(string displayName) 
            :base(displayName)
        {
            //_devices = descriptors.Select(d => GetDeviceByPath(d));
        }
    }
}
