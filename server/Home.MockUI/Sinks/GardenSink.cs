using Lucky.HomeMock.Core;
using System;
using System.IO;
using System.Linq;

namespace Lucky.HomeMock.Sinks
{
    class GardenSink : SinkMockBase
    {
        private byte[] _times = new byte[] { 2, 3, 4, 5, 6 };

        public GardenSink() : base("GARD")
        {
        }

        public override void Read(BinaryReader reader)
        {
            // Read new program
            short count = reader.ReadInt16();
            _times = reader.ReadBytes(count);
            NewConfig?.Invoke(this, new ItemEventArgs<string>(string.Format("Garden timer: {0)", string.Join(", ", _times.Select(t => t.ToString())))));
        }

        public event EventHandler<ItemEventArgs<string>> NewConfig;

        public override void Write(BinaryWriter writer)
        {
            // Returns always off
            writer.Write((byte)0);
            // Returns 5 lines
            writer.Write((short)_times.Length);
            writer.Write(_times);
        }
    }
}
