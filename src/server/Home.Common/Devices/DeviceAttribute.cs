using System;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Decorates a device type
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DeviceAttribute : Attribute
    {
        /// <summary>
        /// Unique display name
        /// </summary>
        public string Name { get; set; }

        public DeviceAttribute() { }

        public DeviceAttribute(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Declared strong dependency to a sink type
    /// </summary>
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

    /// <summary>
    /// Declared strong dependency to a list of sinks of the given type
    /// </summary>
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