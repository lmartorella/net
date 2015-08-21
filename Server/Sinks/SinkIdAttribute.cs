using System;

namespace Lucky.Home.Sinks
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SinkIdAttribute : Attribute
    {
        public SinkIdAttribute(string sinkFourCC)
        {
            SinkFourCC = sinkFourCC;
        }

        public string SinkFourCC { get; private set; }
    }
}
