using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Lucky.HomeMock.Core;

namespace Lucky.HomeMock.Sinks
{
    abstract class SinkBase
    {
        public readonly string FourCc;

        protected SinkBase(string fourCc)
        {
            FourCc = fourCc;
        }

        public abstract void Read(BinaryReader reader);
        public abstract void Write(BinaryWriter writer);
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
