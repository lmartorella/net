using System;

namespace Lucky.Home.Serialization
{
    /// <summary>
    /// Skip serialization of this field
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class NoSerializeAttribute : Attribute
    {

    }
}
