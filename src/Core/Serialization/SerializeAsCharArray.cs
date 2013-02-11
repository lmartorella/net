using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucky.Home.Core.Serialization
{
    [AttributeUsage(AttributeTargets.Field)]
    class SerializeAsCharArrayAttribute : Attribute
    {
        public SerializeAsCharArrayAttribute(int size)
        {
            Size = size;
        }

        public int Size { get; private set; }
    }
}
