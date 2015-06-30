using System;

namespace Lucky.Home.Core
{
    [AttributeUsage(AttributeTargets.Field)]
    class SerializeAsFixedStringAttribute : Attribute
    {
        public SerializeAsFixedStringAttribute(int size)
        {
            Size = size;
        }

        public int Size { get; private set; }
    }
}
