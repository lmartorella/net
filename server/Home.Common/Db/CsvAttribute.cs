using System;

namespace Lucky.Db
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CsvAttribute : Attribute
    {
        public CsvAttribute(string format)
        {
            Format = format;
        }

        public string Format { get; private set; }
    }
}
