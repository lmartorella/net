using System;
using System.Runtime.Serialization;
using Lucky.Home.Devices;

namespace Lucky.Home.Admin
{
    static class MessageSerializerFactory
    {
        public static DataContractSerializer MessageRequestSerialier
        {
            get
            {
                return new DataContractSerializer(typeof(MessageRequest), new Type[0]);
            }
        }

        public static DataContractSerializer MessageResponseSerialier
        {
            get
            {
                return new DataContractSerializer(typeof(MessageResponse), new[] { typeof(Node), typeof(Node[]), typeof(DeviceDescriptor), typeof(DeviceDescriptor[]) });
            }
        }
    }
}
