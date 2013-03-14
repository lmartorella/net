using System;

namespace Lucky.Home.Core.Serialization
{
    [AttributeUsage(AttributeTargets.Field)]
    class SerializeAsFixedArrayAttribute : Attribute
    {
        public SerializeAsFixedArrayAttribute(int size)
        {
            Size = size;
        }

        public int Size { get; private set; }
    }
}
