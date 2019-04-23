using System;

namespace Lucky.Home.Sinks
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SinkIdAttribute : Attribute
    {
        public SinkIdAttribute(string sinkFourCc)
        {
            SinkFourCc = sinkFourCc;
        }

        public string SinkFourCc { get; private set; }
    }
}
