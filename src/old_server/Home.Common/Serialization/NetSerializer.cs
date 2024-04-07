using Lucky.Home.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Lucky.Home.Serialization
{
    /// <summary>
    /// Class to serialize/deserialize a complex .NET type using decorators on fields
    /// </summary>
    public class NetSerializer<T>
    {
        private static readonly TypeSerializer s_directSerializer;

        static NetSerializer()
        {
            s_directSerializer = new TypeSerializer(null, typeof(T));
        }

        public static async Task<T> Read(Stream reader)
        {
            return (T)(await s_directSerializer.Deserialize(reader, null));
        }

        public static Task Write(Stream writer, T value)
        {
            if (value.GetType() != typeof(T))
            {
                throw new NotSupportedException("Inherited types not supported");
            }
            return s_directSerializer.Serialize(writer, value, null);
        }
    }
}
