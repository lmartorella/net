using System;

namespace Lucky.Home.Serialization
{
    /// <summary>
    /// Special exceptions for array with UINT16 lenght at head
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class DynArrayCaseAttribute : Attribute
    {
        public short Key;
        public Type ExcType;
    }
}
