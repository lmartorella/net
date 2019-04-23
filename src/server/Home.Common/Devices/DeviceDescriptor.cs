using System;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Serialize the registered device instance
    /// </summary>
    [DataContract]
    internal class DeviceDescriptor
    {
        [DataMember] 
        public Guid Id;

        [DataMember]
        public string DeviceTypeName;

        [DataMember]
        public object[] Arguments;

        [DataMember]
        public SinkPath[] SinkPaths;
    }
}