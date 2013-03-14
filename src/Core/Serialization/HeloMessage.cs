using System;
using System.Net;

namespace Lucky.Home.Core.Serialization
{
    class HeloMessage
    {
        [SerializeAsFixedArray(8)]
        public string Preamble;

        public Guid DeviceId;

        public const string PreambleValue = "HOMEHELO";
    }

    class HeloAckMessage
    {
        [SerializeAsFixedArray(8)]
        public string Preamble;

        public IPAddress ServerAddress;
        public ushort ServerPort;

        public const string PreambleValue = "HOMEHERE";
    }
}
