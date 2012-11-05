using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Lucky.Home.Core
{
    /// <summary>
    /// Base class for sinks
    /// </summary>
    class Sink
    {
        private static Dictionary<int, Type> s_deviceIds = new Dictionary<int, Type>();

        internal static void RegisterSinkDevice<T>(int deviceId) where T : Sink, new()
        {
            // Exception if already registered..
            s_deviceIds.Add(deviceId, typeof(T));
        }

        internal static void RegisterAssembly(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes().Where(t => typeof(Sink).IsAssignableFrom(t)))
            {
                DeviceIdAttribute[] attr = (DeviceIdAttribute[])type.GetCustomAttributes(typeof(DeviceIdAttribute), false);
                if (attr.Length > 1)
                {
                    s_deviceIds.Add(attr[0].DeviceId, type);
                }
            }
        }

        public static Sink CreateSink(int deviceId)
        {
            Type type;
            if (!s_deviceIds.TryGetValue(deviceId, out type))
            {
                // Unknown sink type
                return null;
            }
            return (Sink)Activator.CreateInstance(type);
        }

        internal void Initialize(short deviceCaps, int servicePort)
        {
            Port = servicePort;
            DeviceCapabilities = deviceCaps;
            OnInitialize();
        }

        protected virtual void OnInitialize()
        { }

        /// <summary>
        /// Get the service port
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Get the device Caps flags
        /// </summary>
        protected short DeviceCapabilities { get; private set; }
    }
}
