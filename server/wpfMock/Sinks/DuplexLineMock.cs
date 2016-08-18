using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucky.HomeMock.Sinks
{
    class DuplexLineMock : SinkMockBase
    {
        private Random _rnd = new Random();

        public DuplexLineMock()
            :base("SLIN")
        { }

        public override void Read(BinaryReader reader)
        {
            var msg = reader.ReadByte();
            if (msg != 2) // RECEIVE
            {
                throw new NotImplementedException();
            }
            // Ok, receive will follow
        }

        public override void Write(BinaryWriter writer)
        {
            // Use three random numbers
            double power = _rnd.NextDouble() * 1000 + 500;
            double current = power / 100;
            double voltage = _rnd.NextDouble() * 5 + 220;
            double[] data = new[] { power, current, voltage };
            var buffer = Encoding.ASCII.GetBytes(string.Join(",", data.Select(d => d.ToString())));

            // Send buffer len
            writer.Write((short)buffer.Length);
            writer.Write(buffer);
        }
    }
}
