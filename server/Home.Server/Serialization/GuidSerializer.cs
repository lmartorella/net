using System;
using System.IO;
using System.Threading.Tasks;

// ReSharper disable StaticMemberInGenericType

namespace Lucky.Home.Serialization
{
    class GuidSerializer : ByteArraySerializer
    {
        private const int Size = 16;

        public GuidSerializer(string fieldName)
            :base(fieldName, Size)
        { }

        public override Task Serialize(Stream writer, object guid, object instance)
        {
            byte[] bytes = ((Guid)guid).ToByteArray();
            return base.Serialize(writer, bytes, instance);
        }

        public override async Task<object> Deserialize(Stream reader, object instance)
        {
            return new Guid((byte[])await base.Deserialize(reader, instance));
        }
    }
}
