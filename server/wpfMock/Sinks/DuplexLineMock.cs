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
            throw new NotImplementedException();
        }

        public override void Write(BinaryWriter writer)
        {
            // Use three random numbers
            double power = _rnd.NextDouble() * 1000 + 500;
            double current = power / 100;
            double voltage = _rnd.NextDouble() * 5 + 220;
            double[] data = new[] { power, current, voltage };
            writer.Write(Encoding.ASCII.GetBytes(string.Join(",", data.Select(d => d.ToString()))));
        }
    }
}
