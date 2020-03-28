using Lucky.Home.IO;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Serialization
{
    /// <summary>
    /// Serialize a .NET string
    /// </summary>
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
                if (await stream.SafeWriteAsync(BitConverter.GetBytes((ushort)size), 2) < 2)
                {
                    return;
                }
            }

            var buffer = Encoding.ASCII.GetBytes(str.ToCharArray(), 0, size);
            await stream.SafeWriteAsync(buffer, buffer.Length);
        }

        public async Task<object> Deserialize(Stream reader, object instance)
        {
            int size = _forcedSize;
            if (size <= 0)
            {
                // Read size
                byte[] buf = new byte[2];
                var b = await reader.SafeReadAsync(buf, 2);
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
            int l = await reader.SafeReadAsync(buffer, size);
            if (l < size)
            {
                throw new BufferUnderrunException(size, _fieldName);
            }
            return Encoding.ASCII.GetString(buffer, 0, size);
        }
    }
}
