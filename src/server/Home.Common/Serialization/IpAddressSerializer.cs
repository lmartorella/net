﻿using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Lucky.Home.Serialization
{
    /// <summary>
    /// Serialize a IPv4 address
    /// </summary>
    class IpAddressSerializer : ByteArraySerializer
    {
        private const int Size = 4;

        public IpAddressSerializer(string fieldName)
            :base(fieldName, Size)
        {
        }

        public override Task Serialize(Stream writer, object address, object instance)
        {
            byte[] chars = ((IPAddress)address).GetAddressBytes();
            return base.Serialize(writer, chars, instance);
        }

        public override async Task<object> Deserialize(Stream reader, object instance)
        {
            return new IPAddress((byte[])await base.Deserialize(reader, instance));
        }
    }
}
