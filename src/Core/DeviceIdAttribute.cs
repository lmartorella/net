using System;

namespace Lucky.Home.Core
{
    [AttributeUsage(AttributeTargets.Class)]
    class DeviceIdAttribute : Attribute
    {
        public DeviceIdAttribute(int deviceId)
        {
            DeviceId = deviceId;
        }

        public int DeviceId { get; private set; }
    }
}
