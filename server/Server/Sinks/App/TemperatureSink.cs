// ReSharper disable once UnusedMember.Global

namespace Lucky.Home.Sinks.App
{
    [SinkId("TEMP")]
    class TemperatureSink : SinkBase
    {
        public byte[] Read()
        {
            byte[] data = null;
            Read(reader =>
            {
                data = reader.ReadBytes(6);
            });
            return data;
        }
    }
}
