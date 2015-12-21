using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    static class MessageSerializerFactory
    {
        public static DataContractSerializer MessageRequestSerialier
        {
            get
            {
                return new DataContractSerializer(typeof(MessageRequest), new[] { typeof(Node), typeof(Node[]) });
            }
        }

        public static DataContractSerializer MessageResponseSerialier
        {
            get
            {
                return new DataContractSerializer(typeof(MessageResponse), new[] { typeof(Node), typeof(Node[]) });
            }
        }
    }
}
