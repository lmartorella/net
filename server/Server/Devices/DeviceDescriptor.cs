using System.Runtime.Serialization;

namespace Lucky.Home.Devices
{
    [DataContract]
    public class DeviceDescriptor
    {
        [DataMember]
        public string DeviceType;

        [DataMember]
        public string Argument;

        [DataMember]
        public SinkPath SinkPath;
    }
}