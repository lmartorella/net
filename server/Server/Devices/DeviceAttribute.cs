using System;

namespace Lucky.Home.Devices
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DeviceAttribute : Attribute
    {
        public Type[] RequiredSinkTypes { get; private set; }

        public DeviceAttribute(Type[] requiredSinkTypes)
        {
            RequiredSinkTypes = requiredSinkTypes;
        }
    }
}