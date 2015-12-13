using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    [DataContract]
    [KnownType(typeof(GetTopologyMessage))]
    [KnownType(typeof(RenameNodeMessage))]
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