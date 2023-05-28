using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    /// <summary>
    /// Factory for data contract serializer of messages
    /// </summary>
    static class MessageSerializerFactory
    {
        private static readonly Type[] KnownTypes;

        static MessageSerializerFactory()
        {
            KnownTypes = new[] { typeof(Node), typeof(Node[]), typeof(NodeId) };
        }

        public static DataContractSerializer MessageRequestSerialier
        {
            get
            {
                return new DataContractSerializer(typeof(MessageRequest), KnownTypes);
            }
        }

        public static DataContractSerializer MessageResponseSerialier
        {
            get
            {
                return new DataContractSerializer(typeof(MessageResponse), KnownTypes);
            }
        }
    }
}
