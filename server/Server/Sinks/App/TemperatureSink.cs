using System.Threading.Tasks;

// ReSharper disable once UnusedMember.Global

namespace Lucky.Home.Sinks.App
{
    [SinkId("TEMP")]
    class TemperatureSink : SinkBase
    {
        public async Task<byte[]> Read()
        {
            byte[] data = null;
            await Read(reader =>
            {
                data = reader.ReadBytes(6);
            });
            return data;
        }
    }
}
