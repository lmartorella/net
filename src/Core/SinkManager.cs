using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lucky.Home.Core
{
    class SinkManager : ServiceBase
    {
        private Dictionary<int, Type> _deviceIds = new Dictionary<int, Type>();

        internal void RegisterSinkDevice<T>(int deviceId) where T : Sink, new()
        {
            // Exception if already registered..
            _deviceIds.Add(deviceId, typeof(T));
        }

        internal void RegisterAssembly(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes().Where(t => typeof(Sink).IsAssignableFrom(t)))
            {
                DeviceIdAttribute[] attr = (DeviceIdAttribute[])type.GetCustomAttributes(typeof(DeviceIdAttribute), false);
                if (attr.Length > 1)
                {
                    _deviceIds.Add(attr[0].DeviceId, type);
                }
            }
        }

        public Sink CreateSink(int deviceId)
        {
            Type type;
            if (!_deviceIds.TryGetValue(deviceId, out type))
            {
                // Unknown sink type
                return null;
            }
            return (Sink)Activator.CreateInstance(type);
        }
    }
}
