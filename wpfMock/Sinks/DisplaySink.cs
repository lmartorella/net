using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lucky.HomeMock.Sinks
{
    class DisplaySink : SinkBase
    {
        public DisplaySink(ushort port)
            :base(port, 0, 1)
        {   }

        protected override void OnSocketOpened(Stream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream))
            {
                int l = reader.ReadInt16();
                string str = ASCIIEncoding.ASCII.GetString(reader.ReadBytes(l));
                if (Data != null)
                {
                    Data(this, new DataEventArgs(str));
                }
            }
        }

        public event EventHandler<DataEventArgs> Data;
    }
}
