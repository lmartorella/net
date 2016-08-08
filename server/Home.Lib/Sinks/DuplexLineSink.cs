using Lucky.Home.Serialization;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Sink for duplex serial line, 9600,N,1
    /// </summary>
    [SinkId("SLIN")]
    class DuplexLineSink : SinkBase
    {
        private enum MessageType : byte
        {
            Send = 1,
            Receive = 2
        }

        private class MessageBase
        {
            public MessageType Type;            
        }

        private class SendMessage : MessageBase
        {
            public SendMessage()
            {
                Type = MessageType.Send;
            }

            [SerializeAsDynArray]
            public byte[] Data;
        }

        private class ReceiveMessage : MessageBase
        {
            public ReceiveMessage()
            {
                Type = MessageType.Receive;
            }
        }

        private class ReceiveResponse
        {
            [SerializeAsDynArray]
            public byte[] Data;
        }

        public void Send(byte[] data)
        {
            Write(writer =>
            {
                writer.Write(new SendMessage { Data = data });
            });
        }

        public byte[] Receive()
        {
            Write(writer =>
            {
                writer.Write(new ReceiveMessage());
            });
            byte[] data = null;
            Read(reader =>
            {
                data = reader.Read<ReceiveResponse>().Data;
            });
            return data;
        }
    }
}
