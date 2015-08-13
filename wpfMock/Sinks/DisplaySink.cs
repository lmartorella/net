using System;
using System.IO;
using System.Text;
using Lucky.HomeMock.Core;

namespace Lucky.HomeMock.Sinks
{
    internal class SystemSink : SinkBase
    {
        public SystemSink()
            : base("SYS ")
        { }

        public override void Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public override void Write(BinaryWriter writer)
        {
            WriteFourcc(writer, "REST");
            writer.Write((ushort)1);    // Brown-out reset
            WriteFourcc(writer, "EOMD");
        }
    }

    class DisplaySink : SinkBase
    {
        public DisplaySink()
            : base("LINE")
        { }

        public override void Read(BinaryReader reader)
        {
            var l = reader.ReadInt16();
            string str = Encoding.ASCII.GetString(reader.ReadBytes(l));
            if (Data != null)
            {
                Data(this, new ItemEventArgs<string>(str));
            }
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((short)1);
            writer.Write((short)20);
        }

        public event EventHandler<ItemEventArgs<string>> Data;
    }
}
