using System;
using System.Runtime.Serialization;
using Lucky.Home.Devices;

namespace Lucky.Home.Admin
{
    static class MessageSerializerFactory
    {
        private static readonly Type[] KnownTypes;

        static MessageSerializerFactory()
        {
            KnownTypes = new[] { typeof(Node), typeof(Node[]), typeof(DeviceDescriptor), typeof(DeviceDescriptor[]), typeof(DeviceTypeDescriptor), typeof(DeviceTypeDescriptor[]) };
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
