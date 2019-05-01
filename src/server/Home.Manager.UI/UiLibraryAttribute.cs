using System;

namespace Lucky.Home
{
    [AttributeUsage(AttributeTargets.Assembly)]
    public class UiLibraryAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class MockSinkAttribute : Attribute
    {
        public MockSinkAttribute(string fourCc, string name)
        {
            FourCc = fourCc;
            Name = name;
        }

        public string FourCc { get; }
        public string Name { get; }
    }
}
