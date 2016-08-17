using System;

namespace Lucky.Home.Db
{
    class FsTimeSeries<T> : ITimeSeries<T>
    {
        public FsTimeSeries(string folderPath)
        {
        }

        internal void Rotate()
        {
        }
    }
}
