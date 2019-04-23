using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    [DataContract]
    internal class MessageRequest
    {
        [DataMember]
        public string Method { get; set; }

        [DataMember]
        public object[] Arguments { get; set; }

        public static DataContractSerializer DataContractSerializer
        {
            get
            {
                return MessageSerializerFactory.MessageRequestSerialier;
            }
        }
    }
}