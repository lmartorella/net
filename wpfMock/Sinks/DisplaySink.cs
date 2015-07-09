using System;
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

        public event EventHandler<ItemEventArgs<string>> Data;
    }
}
