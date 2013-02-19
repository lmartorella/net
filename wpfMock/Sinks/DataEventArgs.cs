using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
