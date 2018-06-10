using Lucky.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Lucky.Serialization
{
    /// <summary>
    /// Class to serialize/deserialize a complex type
    /// </summary>
    public class NetSerializer<T>
    {
        private static readonly TypeSerializer s_directSerializer;
        private static readonly ILogger s_logger;

        static NetSerializer()
        {
            s_directSerializer = new TypeSerializer(null, typeof(T));
            s_logger = Manager.GetService<LoggerFactory>().Create("NetSerializer");
        }

        public static async Task<T> Read(Stream reader)
        {
            try
            {
                return (T)(await s_directSerializer.Deserialize(reader, null));
            }
            catch (BufferUnderrunException exc)
            {
                s_logger.Error(exc.Message);
                return default(T);
            }
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
