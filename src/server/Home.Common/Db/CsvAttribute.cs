﻿using System;

namespace Lucky.Db
{
    [AttributeUsage(AttributeTargets.Field)]
    public class CsvAttribute : Attribute
    {
        public CsvAttribute(string format = null)
        {
            Format = format;
        }

        public string Format { get; private set; }
    }
}
