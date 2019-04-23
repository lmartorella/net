using Lucky.Home.Serialization;

#pragma warning disable 649

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// Brodcast message sent in UDP by nodes for discovery
    /// </summary>
    internal class HeloMessage
    {
        /// <summary>
        /// HOME
        /// </summary>
        public Fourcc Preamble;

        internal const string PreambleValue = "HOME";

        /// <summary>
        /// HEL4 or HTBT or CCHN
        /// </summary>
        public Fourcc MessageCode;

        internal const string HeloMessageCode = "HEL4";
        internal const string HeartbeatMessageCode = "HTBT";
        internal const string SubNodeChanged = "CCHN";
        
        /// <summary>
        /// Node ID (GUID or string)
        /// </summary>
        public NodeId NodeId;

        /// <summary>
        /// Control TCP port
        /// </summary>
        public ushort ControlPort;
    }
}
