using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace Lucky.Home.Core.Serialization
{
    class HeloMessage
    {
        [SerializeAsCharArray(8)]
        public string Preamble;

        public Guid DeviceId;

        public const string PreambleValue = "HOMEHELO";
    }

    class HeloAckMessage
    {
        [SerializeAsCharArray(8)]
        public string Preamble;

        public IPAddress ServerAddress;
        public ushort ServerPort;

        public const string PreambleValue = "HOMEHERE";
    }
}
