using System;

namespace Lucky.Home.Db
{
    class Sample<T>
    {
        T Value { get; }
        T PeakValue { get; }
        DateTime PeakTime { get; }
    }
}
