namespace Lucky.Home.Security
{
    public enum AlarmStatus
    {
        /// <summary>
        /// Unaremd
        /// </summary>
        Unarmed,

        /// <summary>
        /// Armed, not triggered
        /// </summary>
        Armed,

        /// <summary>
        /// Armed, in pre-alarm
        /// </summary>
        PreAlarm,

        /// <summary>
        /// Armed, Alarm
        /// </summary>
        Alarm
    }
}