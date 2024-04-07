using System.Runtime.Serialization;

namespace Lucky.Home.Admin
{
    /// <summary>
    /// Response message
    /// </summary>
    [DataContract]
    internal class MessageResponse
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