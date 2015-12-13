using System;
using System.IO;
using System.Text;
using Lucky.HomeMock.Core;

namespace Lucky.HomeMock.Sinks
{
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
