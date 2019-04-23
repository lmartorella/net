using System;

namespace Lucky.Serialization
{
    public interface IFixedArrayAttribute
    {
        int Size { get; }
    }

    /// <summary>
    /// Used for array 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeAsFixedArrayAttribute : Attribute, IFixedArrayAttribute
    {
        public SerializeAsFixedArrayAttribute(int size)
        {
            Size = size;
        }

        public int Size { get; private set; }
    }

    /// <summary>
    /// Used for strings
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeAsFixedStringAttribute : Attribute, IFixedArrayAttribute
    {
        public SerializeAsFixedStringAttribute(int size)
        {
            Size = size;
        }

        public int Size { get; private set; }
    }
}
