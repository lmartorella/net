using System;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Declare the 4-cc code of the sink
    /// </summary>
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
