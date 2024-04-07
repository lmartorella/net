using System;
using System.Reflection;

namespace Lucky.Home.Serialization
{
    /// <summary>
    /// Array with UINT16 lenght at head
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeAsDynArrayAttribute : Attribute
    {
    }

    /// <summary>
    /// String with UINT16 lenght at head
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SerializeAsDynStringAttribute : Attribute
    {
    }

    /// <summary>
    /// Used to set another field in case of some size found during deserialization
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class DynArrayCaseAttribute : Attribute
    {
        public DynArrayCaseAttribute(int sizeCase, string fieldName, object fieldValue)
        {
            if (sizeCase >= 0)
            {
                throw new NotSupportedException("Only negative special cases supported");
            }
            SizeCase = sizeCase;
            FieldName = fieldName;
            FieldValue = fieldValue;
        }

        public int SizeCase { get; }
        public string FieldName { get; }
        public object FieldValue { get; }

        internal FieldInfo FieldInfo { get; set; }
    }
}

