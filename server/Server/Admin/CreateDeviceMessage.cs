using System.Runtime.Serialization;
using Lucky.Home.Devices;

namespace Lucky.Home.Admin
{
    [DataContract]
    public class CreateDeviceMessage : AdminMessage
    {
        [DataMember]
        public SinkPath SinkPath { get; set; }

        [DataMember]
        public string DeviceType { get; set; }
        
        [DataMember]
        public string Argument { get; set; }

        [DataContract]
        public class Response
        {
            [DataMember]
            public string Error { get; set; }
        }
    }
}