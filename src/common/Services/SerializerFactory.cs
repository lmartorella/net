using System.Runtime.Serialization.Json;
using System.Text;

namespace Lucky.Home.Services;

public class SerializerFactory
{
    public class TypeSerializer<T>(bool indent = false)
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
                using (var stream = new MemoryStream())
                {
                    using (var writer = JsonReaderWriterFactory.CreateJsonWriter(stream, Encoding.UTF8, false, indent, "  "))
                    {
                        serializer.WriteObject(writer, value);
                    }
                    return stream.ToArray();
                }
            }
        }

        public string ToString(T? value)
        {
            return Encoding.UTF8.GetString(Serialize(value));
        }

        public T? Deserialize(byte[]? msg)
        {
            if (msg != null && msg.Length > 0)
            {
                return (T?)serializer.ReadObject(new MemoryStream(msg));
            }
            else
            {
                return default;
            }
        }

        public T? Deserialize(string? msg)
        {
            if (msg != null && msg.Length > 0)
            {
                return Deserialize(Encoding.UTF8.GetBytes(msg));
            }
            else
            {
                return default;
            }
        }
    }

    public TypeSerializer<T> Create<T>(bool indent = false)
    {
        return new TypeSerializer<T>(indent);
    }
}
