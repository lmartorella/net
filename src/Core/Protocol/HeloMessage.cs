using System;
using System.Net;

// ReSharper disable UnassignedField.Global

namespace Lucky.Home.Core.Protocol
{
    public class HeloMessage
    {
        /// <summary>
        /// HOME
        /// </summary>
        [SerializeAsFixedArray(4)]
        public string Preamble;

        internal const string PreambleValue = "HOME";

        /// <summary>
        /// HEL3 or HTBT
        /// </summary>
        [SerializeAsFixedArray(4)]
        public string MessageCode;

        internal const string HeloMessageCode = "HEL3";
        internal const string HeartbeatMessageCode = "HTBT";

        /// <summary>
        /// Device ID
        /// </summary>
        public Guid DeviceId;

        /// <summary>
        /// Control port
        /// </summary>
        public ushort AckPort;
    }

    //public class HeloAckMessage
    //{
    //    [SerializeAsFixedArray(8)]
    //    public string Preamble;

    //    public IPAddress ServerAddress;
    //    public ushort ServerPort;

    //    public const string PreambleValue = "HOMEHERE";
    //}
}
