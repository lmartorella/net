using System;

namespace Lucky.Home
{
    /// <summary>
    /// Describe how enum values should serialize/deserialize from a csv file
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class CsvValueAttribute : Attribute
    {
        public CsvValueAttribute(string value = null)
        {
            Value = value;
        }

        public string Value { get; set; }
    }
}
