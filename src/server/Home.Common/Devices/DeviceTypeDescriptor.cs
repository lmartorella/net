using Lucky.Home.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lucky.Home.Devices
{
    class DeviceTypeManager : ServiceBase
    {
        /// <summary>
        /// From type name to device type
        /// </summary>
        private readonly Dictionary<string, DeviceTypeDescriptor> _deviceTypes = new Dictionary<string, DeviceTypeDescriptor>();
        private List<Assembly> _assemblies = new List<Assembly>();
        public DeviceTypeDescriptor[] DeviceTypes
        {
            get
            {
                return _deviceTypes.Values.ToArray();
            }
        }

        public DeviceTypeManager()
        {
            Manager.GetService<Registrar>().AssemblyLoaded += (o, e) =>
            {
                RegisterAssembly(e.Item);
            };
        }

        private void RegisterAssembly(Assembly assembly)
        {
            var l = _deviceTypes.Count;
            _assemblies.Add(assembly);
            foreach (var deviceType in assembly.GetTypes().Where(type => type.BaseType != null && type != typeof(DeviceBase) && typeof(DeviceBase).IsAssignableFrom(type) && type.GetCustomAttribute<DeviceAttribute>() != null))
            {
                var descriptor = new DeviceTypeDescriptor(deviceType);
                // Exception if already registered..
                _deviceTypes.Add(descriptor.Name, descriptor);
            }
            Logger.Log("DeviceType Reg", "Asm", assembly.GetName().Name, "Count", _deviceTypes.Count - l);
        }

        public Type GetDeviceType(string deviceTypeName)
        {
            DeviceTypeDescriptor desc;
            if (!_deviceTypes.TryGetValue(deviceTypeName, out desc))
            {
                // Type not found
                return null;
            }

            // Find type
            var typeName = desc.FullTypeName;
            var type = _assemblies.Select(a => a.GetType(typeName)).FirstOrDefault(t => t != null);
            if (type == null)
            {
                throw new InvalidOperationException("Devcice type not found: " + typeName);
            }

            return type;
        }
    }

    /// <summary>
    /// Serialize the device type
    /// </summary>
    internal class DeviceTypeDescriptor
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

        public string FullTypeName { get; set; }

        public string Name { get; set; }

        public string[] ArgumentNames { get; set; }

        public string[] ArgumentTypes { get; set; }
    }
}