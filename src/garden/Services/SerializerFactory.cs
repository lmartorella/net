using System.Runtime.Serialization.Json;

namespace Lucky.Garden.Services
{
    public class SerializerFactory
    {
        public class TypeSerializer<T>
        {
            private readonly DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(T));
            
            public byte[] Serialize(T? value)
            {
                if (value == null)
                {
                    return [];
                }
                else
                {
                    var stream = new MemoryStream();
                    serializer.WriteObject(stream, value);
                    return stream.ToArray();
                }
            }

            public T? Deserialize(byte[]? msg)
            {
                if (msg != null && msg.Length> 0)
                {
                    return (T?)serializer.ReadObject(new MemoryStream(msg));
                }
                else
                {
                    return default;
                }
            }
        }

        public TypeSerializer<T> Create<T>()
        {
            return new TypeSerializer<T>();
        }
    }

}