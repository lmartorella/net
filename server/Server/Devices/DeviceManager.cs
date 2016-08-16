using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Lucky.Services;

namespace Lucky.Home.Devices
{
    class DeviceManager : ServiceBaseWithData<DeviceManager.Persistence>, IDeviceManager
    {
        /// <summary>
        /// From type name to device type
        /// </summary>
        private readonly Dictionary<string, DeviceTypeDescriptor> _deviceTypes = new Dictionary<string, DeviceTypeDescriptor>();
        private readonly Dictionary<Guid, Tuple<IDevice, DeviceDescriptor>> _devices = new Dictionary<Guid, Tuple<IDevice, DeviceDescriptor>>();
        private List<Assembly> _assemblies = new List<Assembly>();

        [DataContract]
        internal class Persistence
        {
            [DataMember]
            public DeviceDescriptor[] Descriptors { get; set; }
        }

        public DeviceTypeDescriptor[] DeviceTypes
        {
            get
            {
                return _deviceTypes.Values.ToArray();
            }
        }

        public void RegisterAssembly(Assembly assembly)
        {
            _assemblies.Add(assembly);
            foreach (var deviceType in assembly.GetTypes().Where(type => type.BaseType != null && type != typeof(DeviceBase) && typeof(DeviceBase).IsAssignableFrom(type) && type.GetCustomAttribute<DeviceAttribute>() != null))
            {
                var descriptor = new DeviceTypeDescriptor(deviceType);
                // Exception if already registered..
                _deviceTypes.Add(descriptor.Name, descriptor);
            }
        }

        private void LoadState(DeviceDescriptor[] descriptors)
        {
            lock (_devices)
            {
                if (descriptors != null)
                {
                    foreach (var descriptor in descriptors)
                    {
                        CreateDevice(descriptor);
                    }
                }
            }
        }

        private IDeviceInternal CreateDevice(DeviceDescriptor descriptor)
        {
            // Find type
            var typeName = _deviceTypes[descriptor.DeviceTypeName].FullTypeName;
            var type = _assemblies.Select(a => a.GetType(typeName)).FirstOrDefault(t => t != null);
            if (type == null)
            {
                throw new InvalidOperationException("Devcice type not found: " + typeName);
            }

            IDeviceInternal device = (IDeviceInternal)Activator.CreateInstance(type, descriptor.Arguments);
            device.OnInitialize(descriptor.SinkPaths);
            descriptor.Id = Guid.NewGuid();
            lock (_devices)
            {
                _devices.Add(descriptor.Id, Tuple.Create((IDevice)device, descriptor));
            }
            return device;
        }

        private void SaveState()
        {
            State = new Persistence
            {
                Descriptors = GetDeviceDescriptors()
            };
        }

        public IDevice CreateAndLoadDevice(DeviceDescriptor descriptor)
        {
            var device = CreateDevice(descriptor);
            SaveState();
            return device;
        }

        public DeviceDescriptor[] GetDeviceDescriptors()
        {
            lock (_devices)
            {
                return _devices.Values.Select(d => d.Item2).ToArray();
            }
        }

        public void Load()
        {
            LoadState(State.Descriptors);
        }

        public void DeleteDevice(Guid id)
        {
            lock (_devices)
            {
                Tuple<IDevice, DeviceDescriptor> tuple;
                if (_devices.TryGetValue(id, out tuple))
                {
                    tuple.Item1.Dispose();
                    _devices.Remove(id);
                }
            }
            SaveState();
        }
    }
}