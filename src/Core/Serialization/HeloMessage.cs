using System;
using System.Net;

namespace Lucky.Home.Core.Serialization
{
    public class HeloMessage
    {
        [SerializeAsFixedArray(8)]
        public string Preamble;
        public Guid DeviceId;
        public ushort AckPort;

        public const string PreambleValue = "HOMEHEL2";
    }

    public class HeloAckMessage
    {
        [SerializeAsFixedArray(8)]
        public string Preamble;

        public IPAddress ServerAddress;
        public ushort ServerPort;

        public const string PreambleValue = "HOMEHERE";
    }
}
