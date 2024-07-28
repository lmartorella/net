﻿using System;

namespace Lucky.Home
{
    /// <summary>
    /// Describe field to serialize/deserialize from a csv file
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CsvAttribute : Attribute
    {
        public CsvAttribute(string format = null)
        {
            Format = format;
        }

        public string Name { get; set; }

        public string Format { get; private set; }

        public bool OnlyForParsing { get; set; }
    }
}
