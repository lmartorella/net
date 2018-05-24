using System;
using System.IO;
using System.Threading.Tasks;

// ReSharper disable StaticMemberInGenericType

namespace Lucky.Home.Serialization
{
    class BitConverterSerializer<T> : ByteArraySerializer where T : struct
    {
        private readonly Func<T, byte[]> _ser;
        private readonly Func<byte[], T> _deser;

        public BitConverterSerializer(int size, Func<T, byte[]> ser, Func<byte[], T> deser, string fieldName)
            :base(fieldName, size)
        {
            _ser = ser;
            _deser = deser;
        }

        public override Task Serialize(Stream writer, object source, object instance)
        {
            return base.Serialize(writer, _ser((T)source), instance);
        }

        public override async Task<object> Deserialize(Stream reader, object instance)
        {
            return _deser((byte[])await base.Deserialize(reader, instance));
        }
    }
}
