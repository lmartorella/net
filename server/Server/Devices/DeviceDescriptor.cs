using System.Runtime.Serialization;

namespace Lucky.Home.Devices
{
    [DataContract]
    public class DeviceDescriptor
    {
        [DataMember]
        public string DeviceType;

        [DataMember]
        public object[] Arguments;

        [DataMember]
        public SinkPath[] SinkPaths;
    }
}