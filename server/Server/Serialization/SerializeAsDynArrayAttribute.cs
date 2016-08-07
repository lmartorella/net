using System;

namespace Lucky.Home.Serialization
{
    /// <summary>
    /// Array with UINT16 lenght at head
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeAsDynArrayAttribute : Attribute
    {
    }
}

