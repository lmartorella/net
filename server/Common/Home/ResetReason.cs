namespace Lucky.Home
{
    public enum ResetReason
    {
        Waiting = -1,
        Power = 1,  // Power-on reset
        Brownout,
        ConfigMismatch,
        Watchdog,
        StackFail,
        MClr,
        Exception
    }
}