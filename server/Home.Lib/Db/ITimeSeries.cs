using System;

namespace Lucky.Home.Db
{
    interface ITimeSeries<T>
    {
        Sample<T> ImmediateData { get; }
        Sample<T> CurrentDayData { get; }
        Sample<T> LastDayData { get; }
        Sample<T> LastWeekData { get; }
        Sample<T> LastMonthData { get; }
    }
}
