using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    /// <summary>
    /// Get node topology as connection tree
    /// </summary>
    [DataContract]
    public class GetTopologyMessage : AdminMessage
    {
        [DataContract]
        public class Response
        {
            [DataMember]
            public Node[] Roots;
        }
    }
}
