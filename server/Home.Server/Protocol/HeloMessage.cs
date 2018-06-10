using Lucky.Serialization;

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

        internal const string HeloMessageCode = "HEL4";
        internal const string HeartbeatMessageCode = "HTBT";
        internal const string SubNodeChanged = "CCHN";
        
        /// <summary>
        /// Node ID
        /// </summary>
        public NodeId NodeId;

        /// <summary>
        /// Control TCP port
        /// </summary>
        public ushort ControlPort;
    }
}
