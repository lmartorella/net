using System;

namespace Lucky.Home.Serialization
{
    public interface IFixedArrayAttribute
    {
        int Size { get; }
    }

    /// <summary>
    /// Declares a fixed-size arrays
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
    /// Declares a fixed-size string
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
