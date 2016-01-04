using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Lucky.Services;

namespace Lucky.Home.Devices
{
    class DeviceManager : ServiceBaseWithData<DeviceManager.Persistence>
    {
        /// <summary>
        /// From type name to device type
        /// </summary>
        private readonly Dictionary<string, DeviceTypeDescriptor> _deviceTypes = new Dictionary<string, DeviceTypeDescriptor>();
        private readonly List<Tuple<IDevice, DeviceDescriptor>> _devices = new List<Tuple<IDevice, DeviceDescriptor>>();

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

        public DeviceManager RegisterAssembly(Assembly assembly)
        {
            foreach (var deviceType in assembly.GetTypes().Where(type => type.BaseType != null && type != typeof(DeviceBase) && typeof(DeviceBase).IsAssignableFrom(type)))
            {
                // Exception if already registered..
                var descriptor = new DeviceTypeDescriptor(deviceType);
                _deviceTypes.Add(descriptor.TypeName, descriptor);
            }
            return this;
        }

        private void LoadState(DeviceDescriptor[] descriptors)
        {
            if (descriptors != null)
            {
                foreach (var descriptor in descriptors)
                {
                    CreateDevice(descriptor);
                }
            }
        }

        private IDeviceInternal CreateDevice(DeviceDescriptor descriptor)
        {
            IDeviceInternal device = (IDeviceInternal)Activator.CreateInstance(_deviceTypes[descriptor.DeviceType].Type, descriptor.Arguments);
            device.OnInitialize(descriptor.SinkPaths);
            _devices.Add(Tuple.Create((IDevice)device, descriptor));
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
            return _devices.Select(d => d.Item2).ToArray();
        }

        public void Load()
        {
            LoadState(State.Descriptors);
        }
    }
}