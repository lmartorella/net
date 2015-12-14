using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    [DataContract]
    [KnownType(typeof(GetTopologyMessage))]
    [KnownType(typeof(RenameNodeMessage))]
    [KnownType(typeof(GetDeviceTypesMessage))]
    [KnownType(typeof(CreateDeviceMessage))]
    public class Container
    {
        [DataMember]
        public AdminMessage Message;
    }

    [DataContract]
    public class AdminMessage
    {

    }
}