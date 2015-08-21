using System.Collections.Generic;
using System.Linq;
using Lucky.Home.Devices;
using Lucky.Home.Plugin;
using Lucky.Services;

namespace Lucky.Home.Security.Sensors
{
    class Window : SensorBase
    {
        private IEnumerable<IDevice> _devices;

        public Window(string displayName, DeviceDescriptor[] descriptors) 
            :base(displayName)
        {
            _devices = descriptors.Select(d => GetDeviceByPath(d));
        }
    }
}
