namespace Lucky.Home
{
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