using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    [DataContract]
    public class MessageResponse
    {
        [DataMember]
        public object Value { get; set; }

        public static DataContractSerializer DataContractSerializer
        {
            get
            {
                return MessageSerializerFactory.MessageResponseSerialier;
            }
        }
    }
}