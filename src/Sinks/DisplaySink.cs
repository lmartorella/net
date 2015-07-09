using System.IO;
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
        protected override void OnInitialize()
        {
            base.OnInitialize();
            byte[] msg = ReadBytes();
            var metadata = NetSerializer<ReadCapMessageResponse>.Read(new BinaryReader(new MemoryStream(msg)));

            LineCount = metadata.LineCount;
            CharCount = metadata.CharCount;
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ReadCapMessageResponse
        {
            public short LineCount;
            public short CharCount;
        }

        public void Write(string line)
        {
            WriteBytes(Encoding.ASCII.GetBytes(line));
        }

        public int LineCount { get; private set; }
        public int CharCount { get; private set; }
    }
}
