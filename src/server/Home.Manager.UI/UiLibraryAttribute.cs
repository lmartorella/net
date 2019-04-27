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
        public MockSinkAttribute(string fourCc)
        {
            FourCc = fourCc;
        }

        public string FourCc { get; }
    }
}
