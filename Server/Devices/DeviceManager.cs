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
        /// From sink type to device type
        /// </summary>
        private readonly Dictionary<Type, Type> _deviceTypes = new Dictionary<Type, Type>();
        private readonly List<IDevice> _devices = new List<IDevice>();

        [DataContract]
        internal class Persistence
        {
            public DeviceDescriptor[] Descriptors { get; set; }
        }

        [DataContract]
        internal class DeviceDescriptor
        {
            [DataMember]
            public Type DeviceType;

            [DataMember]
            public SinkPath SinkPath;
        }

        public DeviceManager()
        {
            LoadState(State.Descriptors);
        }

        public void RegisterAssembly(Assembly assembly)
        {
            foreach (var deviceType in assembly.GetTypes().Where(type => typeof(DeviceBase).IsAssignableFrom(type) && type.GetCustomAttribute<SupportsAttribute>() != null))
            {
                // Exception if already registered..
                _deviceTypes.Add(deviceType.GetCustomAttribute<SupportsAttribute>().SinkType, deviceType);
            }
        }

        private void LoadState(DeviceDescriptor[] descriptors)
        {
            if (descriptors != null)
            {
                foreach (var descriptor in descriptors)
                {
                    DeviceBase device = (DeviceBase)Activator.CreateInstance(descriptor.DeviceType);
                    device.OnInitialize(descriptor.SinkPath);
                    _devices.Add(device);
                }
            }
        }

        private void SaveState(IDevice[] devices)
        {
            State = new Persistence
            {
                Descriptors = devices.Select(d => new DeviceDescriptor
                {
                    SinkPath = d.SinkPath,
                    DeviceType = d.GetType()
                }).ToArray()
            };
        }

        public void RegisterDevice(DeviceBase device)
        {
            _devices.Add(device);
            SaveState(_devices.ToArray());
        }
    }
}