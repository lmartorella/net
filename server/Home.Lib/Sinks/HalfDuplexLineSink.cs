using Lucky.Home.Serialization;
using System;

#pragma warning disable CS0649

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
            [DynArrayCase(Key = -1, ExcType = typeof(OverflowException))]
            public byte[] RxData;
        }

        public enum Error
        {
            Ok,
            Overflow
        }

        public byte[] SendReceive(byte[] txData, bool wantsResponse, bool echo, string opName, out Error error)
        {
            byte mode = 0;
            if (echo)
            {
                mode = 0xff;
            }
            else if (!wantsResponse)
            {
                mode = 0xfe;
            }
            Write(writer =>
            {
                writer.Write(new Message { TxData = txData, Mode = mode });
            }, opName);

            byte[] data = null;
            Error err = Error.Ok;
            Read(reader =>
            {
                try
                {
                    data = reader.Read<Response>()?.RxData;
                }
                catch (OverflowException)
                {
                    data = new byte[0];
                    err = Error.Overflow;
                }
            }, opName);

            error = err;
            return data;
        }
    }
}
