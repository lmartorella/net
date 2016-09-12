using Lucky.Home.Serialization;
using System;
using System.Threading.Tasks;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Sink for half-duplex serial line: 9600,N,1, 0.1 sec RX timeout
    /// </summary>
    [SinkId("SLIN")]
    class HalfDuplexLineSink : SinkBase
    {
        private class Message
        {
            [SerializeAsDynArray]
            public byte[] TxData;
        }

        private class Response
        {
            [SerializeAsDynArray]
            public byte[] RxData;
        }

        public async Task<byte[]> SendReceive(byte[] txData)
        {
            Write(writer =>
            {
                writer.Write(new Message { TxData = txData });
            });

            // Wait 0.2 sec (192 words)
            await Task.Delay(TimeSpan.FromSeconds(0.2));

            byte[] data = null;
            Read(reader =>
            {
                data = reader.Read<Response>()?.RxData;
            });
            return data;
        }
    }
}
