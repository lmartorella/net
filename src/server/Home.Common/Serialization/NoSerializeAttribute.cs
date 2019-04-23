using System;

namespace Lucky.Serialization
{
    /// <summary>
    /// Skip serialization of this field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class NoSerializeAttribute : Attribute
    {

    }
}
