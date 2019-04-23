using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices
{
    [DataContract]
    public class DeviceTypeDescriptor
    {
        public DeviceTypeDescriptor(Type type)
        {
            FullTypeName = type.FullName;
            Name = type.GetCustomAttribute<DeviceAttribute>().Name;

            var constructors = type.GetConstructors();
            if (constructors.Length != 1)
            {
                throw new InvalidOperationException("Too many constructors in device type " + Name);
            }

            var args = constructors[0].GetParameters();
            ArgumentNames = args.Select(arg => arg.Name).ToArray();
            ArgumentTypes = args.Select(arg => arg.ParameterType.FullName).ToArray();
        }

        [DataMember]
        public string FullTypeName { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string[] ArgumentNames { get; set; }

        [DataMember]
        public string[] ArgumentTypes { get; set; }
    }
}