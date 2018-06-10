using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Serialization
{
    class StringSerializer : ISerializer
    {
        private readonly string _fieldName;
        private readonly int _forcedSize;
 
        public StringSerializer(string fieldName, int forcedSize)
        {
            _fieldName = fieldName;
            _forcedSize = forcedSize;
        }

        public async Task Serialize(Stream stream, object source, object instance)
        {
            string str = (string)source;
            int size = _forcedSize;
            if (size <= 0)
            {
                size = str.Length;
                // Serialize count as word
                await stream.WriteAsync(BitConverter.GetBytes((ushort)size), 0, 2);
            }

            var buffer = Encoding.ASCII.GetBytes(str.ToCharArray(), 0, size);
            await stream.WriteAsync(buffer, 0, buffer.Length);
        }

        public async Task<object> Deserialize(Stream reader, object instance)
        {
            int size = _forcedSize;
            if (size <= 0)
            {
                // Read size
                byte[] buf = new byte[2];
                var b = await reader.ReadAsync(buf, 0, 2);
                if (b < 2)
                {
                    throw new BufferUnderrunException(2, "(sizeof)" + (_fieldName ?? ""));
                }
                size = BitConverter.ToInt16(buf, 0);
                if (size < 0)
                {
                    throw new InvalidOperationException("Dynamic Array with negative size: " + size);
                }
            }

            byte[] buffer = new byte[size];
            int l = await reader.ReadAsync(buffer, 0, size);
            if (l < size)
            {
                throw new BufferUnderrunException(size, _fieldName);
            }
            return Encoding.ASCII.GetString(buffer, 0, size);
        }
    }
}
