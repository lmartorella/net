using System;

namespace Lucky.Home.Devices
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DeviceAttribute : Attribute
    {
        public string Name { get; set; }

        public DeviceAttribute() { }

        public DeviceAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequiresAttribute : Attribute
    {
        public Type Type { get; set; }
        public bool AllowMultiple { get; set; }

        public RequiresAttribute() { }

        public RequiresAttribute(Type sinkType)
        {
            Type = sinkType;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class RequiresArrayAttribute : RequiresAttribute
    {
        public RequiresArrayAttribute() { }

        public RequiresArrayAttribute(Type sinkType)
            :base(sinkType)
        {
            AllowMultiple = true;
        }
    }
}