using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices
{
    [DataContract]
    public class DeviceDescriptor
    {
        [DataMember] 
        public Guid Id;

        [DataMember]
        public string DeviceType;

        [DataMember]
        public object[] Arguments;

        [DataMember]
        public SinkPath[] SinkPaths;
    }
}