using System.Text;
using Lucky.Home.Core;

// ReSharper disable once UnusedMember.Global

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Simple line display protocol
    /// </summary>
    /// <remarks>
    /// Protocol: WRITE: raw ASCII data, no ending zero
    /// </remarks>
    [SinkId("LINE")]
    class DisplaySink : Sink, IDisplaySink
    {
        public void Write(string line)
        {
            WriteBytes(Encoding.ASCII.GetBytes(line));
        }

        public int LineCount { get; private set; }
        public int CharCount { get; private set; }
    }
}
