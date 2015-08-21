using System;

namespace Lucky.Home.Serialization
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
