using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucky.Home.Core.Serialization
{
    [AttributeUsage(AttributeTargets.Field)]
    class SerializeAsDynArrayAttribute : Attribute
    {
        public SerializeAsDynArrayAttribute()
        { }
    }
}

