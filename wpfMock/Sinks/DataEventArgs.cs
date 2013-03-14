using System;

namespace Lucky.HomeMock.Sinks
{
    class DataEventArgs : EventArgs
    {
        public readonly string Str;
        public DataEventArgs(string str)
        {
            Str = str;
        }
    }
}
