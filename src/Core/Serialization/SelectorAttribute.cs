using System;

namespace Lucky.Home.Core.Serialization
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    class SelectorAttribute : Attribute
    {
        public object SelectorValue { get; private set; }

        public Type Type { get; private set; }

        public SelectorAttribute(object selectorValue, Type type)
        {
            SelectorValue = selectorValue;
            Type = type;
        }
    }
}
