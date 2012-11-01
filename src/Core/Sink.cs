using System;
using System.Collections.Generic;
using System.Linq;
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
        }

        /// <summary>
        /// Get the service port
        /// </summary>
        public int Port { get; private set; }
    }
}
