using System;

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
        /// Control TCP port
        /// </summary>
        public ushort ControlPort;
    }
}
