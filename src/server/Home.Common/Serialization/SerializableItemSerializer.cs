using System.IO;
using System.Threading.Tasks;

// ReSharper disable StaticMemberInGenericType

namespace Lucky.Serialization
{
    /// <summary>
    /// Serializes a ISerializable class 
    /// </summary>
    class SerializableItemSerializer<T> : ISerializer where T : ISerializable, new()
    {
        private string _fieldName;

        public SerializableItemSerializer(string fieldName) 
        {
            _fieldName = fieldName;
        }

        public async Task Serialize(Stream writer, object value, object instance)
        {
            var buf = ((ISerializable)value).Serialize();
            await writer.WriteAsync(buf, 0, buf.Length);
        }

        public async Task<object> Deserialize(Stream reader, object instance)
        {
            T ret = new T();
            await ret.Deserialize(async l =>
            {
                byte[] buffer = new byte[l];
                int s = await reader.ReadAsync(buffer, 0, l);
                if (s < l)
                {
                    throw new BufferUnderrunException(l, _fieldName);
                }
                return buffer;
            });
            return ret;
        }
    }
}
