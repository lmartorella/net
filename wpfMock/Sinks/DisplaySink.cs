using System;
using System.Collections.Generic;
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

        public abstract void Read(byte[] data);
        public abstract byte[] Write();
    }

    class DisplaySink : SinkBase
    {
        public DisplaySink()
            : base("LINE")
        { }

        public override void Read(byte[] data)
        {
            string str = Encoding.ASCII.GetString(data);
            if (Data != null)
            {
                Data(this, new ItemEventArgs<string>(str));
            }
        }

        public override byte[] Write()
        {
            List<byte> retValue = new List<byte>();
            retValue.AddRange(BitConverter.GetBytes((short)1));
            retValue.AddRange(BitConverter.GetBytes((short)20));
            return retValue.ToArray();
        }

        public event EventHandler<ItemEventArgs<string>> Data;
    }
}
