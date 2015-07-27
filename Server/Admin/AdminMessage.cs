using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    [DataContract]
    [KnownType(typeof(GetTopologyMessage))]
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