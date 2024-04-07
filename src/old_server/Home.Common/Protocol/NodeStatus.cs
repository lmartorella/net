using System.Runtime.Serialization;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// Node status
    /// </summary>
    [DataContract]
    internal class NodeStatus
    {
        [DataMember]
        public ResetReason ResetReason { get; set; }

        [DataMember]
        public string ExceptionMessage { get; set; }

        public override string ToString()
        {
            if (ResetReason == ResetReason.None)
            {
                return "OK";
            }
            else
            {
                var ret = "Reason: " + ResetReason;
                if (ExceptionMessage != null)
                {
                    ret += ", Exc: " + ExceptionMessage;
                }
                return ret;
            }
        }
    }
}