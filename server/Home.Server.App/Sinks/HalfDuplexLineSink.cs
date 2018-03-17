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

        public enum Error
        {
            Ok,
            Overflow,
            FrameError
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
            }, opName + ":WR");

            byte[] data = null;
            Error err = Error.Ok;
            // The Read operation is actually sending the buffer first and then synchronously (blocking the bus)
            // reading the response (if mode is not 0xfe)
            Read(reader =>
            {
                int size = BitConverter.ToInt16(reader.ReadBytes(2), 0);
                if (size == -1)
                {
                    data = new byte[0];
                    err = Error.Overflow;
                }
                else if (size == -2)
                {
                    data = new byte[0];
                    err = Error.FrameError;
                }
                else
                {
                    data = reader.ReadBytes(size);
                }
            }, opName + ":RD");
            error = err;
            return data;
        }
    }
}
