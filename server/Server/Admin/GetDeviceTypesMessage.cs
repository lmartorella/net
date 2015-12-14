using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    [DataContract]
    public class GetDeviceTypesMessage : AdminMessage
    {
        [DataContract]
        public class Response
        {
            [DataMember]
            public string[] DeviceTypes { get; set; }
        }
    }
}