using System;

namespace Lucky.Home.Devices
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class SupportsAttribute : Attribute
    {
        public Type SinkType { get; private set; }

        public SupportsAttribute(Type sinkType)
        {
            SinkType = sinkType;
        }
    }
}