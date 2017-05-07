using Lucky.Home.Serialization;
using System;
using System.Runtime.CompilerServices;
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
            public byte Mode;

            [SerializeAsDynArray]
            public byte[] TxData;
        }

        private class Response
        {
            [SerializeAsDynArray]
            public byte[] RxData;
        }

        public byte[] SendReceive(byte[] txData, bool echo, string opName)
        {
            Write(writer =>
            {
                writer.Write(new Message { TxData = txData, Mode = echo ? (byte)0xff : (byte)0x00 });
            }, opName);
            byte[] data = null;
            Read(reader =>
            {
                data = reader.Read<Response>()?.RxData;
            }, opName);
            return data;
        }
    }
}
