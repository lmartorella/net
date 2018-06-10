using Lucky.Serialization;
using System.Threading.Tasks;

#pragma warning disable 649

namespace Lucky.Home.Sinks
{
    class TempMessage
    {
        [SerializeAsFixedArray(6)]
        public byte[] Data;
    }

    [SinkId("TEMP")]
    class TemperatureSink : SinkBase
    {
        public async Task<byte[]> Read()
        {
            byte[] data = null;
            await Read(async reader =>
            {
                data = (await reader.Read<TempMessage>()).Data;
            });
            return data;
        }
    }
}
