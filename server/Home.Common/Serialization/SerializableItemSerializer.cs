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
            byte[] buffer = new byte[ret.DataSize];
            int s = await reader.ReadAsync(buffer, 0, ret.DataSize);
            if (s < ret.DataSize)
            {
                throw new BufferUnderrunException(ret.DataSize, _fieldName);
            }
            ret.Deserialize(buffer);
            return ret;
        }
    }
}
