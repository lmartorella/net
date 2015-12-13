using System.Text;
using Lucky.Home.Serialization;

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
    class DisplaySink : SinkBase, IDisplaySink
    {
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Read(reader =>
            {
                var metadata = reader.Read<ReadCapMessageResponse>();
                LineCount = metadata.LineCount;
                CharCount = metadata.CharCount;
            });
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ReadCapMessageResponse
        {
            public short LineCount;
            public short CharCount;
        }

        private class LineMessage
        {
            [SerializeAsDynArray]
            public byte[] Data;
        }

        public void Write(string line)
        {
            Write(writer =>
            {
                writer.Write(new LineMessage { Data = Encoding.ASCII.GetBytes(line)});
            });
        }

        public int LineCount { get; private set; }
        public int CharCount { get; private set; }
    }
}
