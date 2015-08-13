namespace Lucky.Home.Sinks
{
    public class NodeStatus
    {
        public ResetReason ResetReason { get; set; }
        public string ExceptionMessage { get; set; }        
    }

    public enum ResetReason
    {
        Power = 1,  // Power-on reset
        Brownout,
        ConfigMismatch,
        Watchdog,
        StackFail,
        MClr,
        Exception
    }
}