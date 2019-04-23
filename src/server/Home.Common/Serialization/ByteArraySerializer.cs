using System.IO;
using System.Threading.Tasks;

namespace Lucky.Home.Serialization
{
    /// <summary>
    /// Serializer for byte array fields
    /// </summary>
    class ByteArraySerializer : ISerializer
    {
        protected readonly string _fieldName;
        private int _size;

        public ByteArraySerializer(string fieldName, int size = 0)
        {
            _fieldName = fieldName;
            _size = size;
        }

        public virtual Task Serialize(Stream writer, object source, object instance)
        {
            byte[] array = (byte[])source;
            return Serialize(writer, array, _size);
        }

        public Task Serialize(Stream writer, byte[] array, int size)
        {
            return writer.WriteAsync(array, 0, size);
        }

        public virtual async Task<object> Deserialize(Stream reader, object instance)
        {
            return await Deserialize(reader, _size);
        }

        public async Task<byte[]> Deserialize(Stream reader, int size)
        {
            byte[] buffer = new byte[size];
            int l = await reader.ReadAsync(buffer, 0, size);
            if (l < size)
            {
                throw new BufferUnderrunException(size, _fieldName);
            }
            return buffer;
        }
    }
}
