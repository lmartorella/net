using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices
{
    [DataContract]
    public class DeviceTypeDescriptor
    {
        public DeviceTypeDescriptor(Type type)
        {
            Type = type;
            TypeName = Type.Name;

            var constructors = Type.GetConstructors();
            if (constructors.Count() != 1)
            {
                throw new InvalidOperationException("Too many constructors in type " + TypeName);
            }

            var args = constructors.First().GetParameters();
            ArgumentNames = args.Select(arg => arg.Name).ToArray();
            ArgumentTypes = args.Select(arg => arg.ParameterType.FullName).ToArray();
        }

        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public string[] ArgumentNames { get; set; }

        [DataMember]
        public string[] ArgumentTypes { get; set; }

        internal Type Type { get; private set; }
    }
}