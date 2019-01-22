using System;
using System.IO;

namespace Lucky.HomeMock.Sinks
{
    class TemperatureSink : SinkMockBase
    {
        private readonly Random _random;

        public TemperatureSink()
            : base("TEMP")
        {
            _random = new Random();
        }

        public override void Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        private static ushort FromDec(float n)
        {
            int intPart = (int)Math.Floor(n);
            uint decPart = (uint)((n - intPart) * 256.0f);
            return (ushort)(((intPart & 0xff) << 8) + (decPart & 0xff));
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write((byte)1);
            int hum = _random.Next(100);
            float temp = _random.Next(200, 300) / 10.0f;

            var hums = FromDec(hum);
            var temps = FromDec(temp);
            writer.Write(hums);
            writer.Write(temps);

            uint checksum = (uint)((hums & 0xff) + (hums >> 8));
            checksum += (uint)((temps & 0xff) + (temps >> 8));
            writer.Write((byte)checksum);
        }
    }
}
