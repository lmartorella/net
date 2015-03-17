using System;

namespace Lucky.Home.Sinks
{
    [AttributeUsage(AttributeTargets.Class)]
    class SinkIdAttribute : Attribute
    {
        public SinkIdAttribute(SinkTypes sinkType)
        {
            SinkType = sinkType;
        }

        public SinkTypes SinkType { get; private set; }
    }
}
