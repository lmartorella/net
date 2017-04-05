using System;
using Lucky.Home.Serialization;

// ReSharper disable UnassignedField.Global

namespace Lucky.Home.Protocol
{
    public class HeloMessage
    {
        /// <summary>
        /// HOME
        /// </summary>
        public Fourcc Preamble;

        internal const string PreambleValue = "HOME";

        /// <summary>
        /// HEL3 or HTBT
        /// </summary>
        public Fourcc MessageCode;

        internal const string HeloMessageCode = "HEL3";
        internal const string HeartbeatMessageCode = "HTBT";
        internal const string SubNodeChanged = "CCHN";
        
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
