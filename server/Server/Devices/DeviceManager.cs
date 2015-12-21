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
        private readonly Dictionary<string, Type> _deviceTypes = new Dictionary<string, Type>();
        private readonly List<IDevice> _devices = new List<IDevice>();

        [DataContract]
        internal class Persistence
        {
            public DeviceDescriptor[] Descriptors { get; set; }
        }

        public DeviceManager()
        {
            LoadState(State.Descriptors);
        }

        public string[] DeviceTypes
        {
            get
            {
                return _deviceTypes.Keys.ToArray();
            }
        }

        public void RegisterAssembly(Assembly assembly)
        {
            foreach (var deviceType in assembly.GetTypes().Where(type => !type.IsGenericType && type.BaseType != null && type.BaseType.IsGenericType && typeof(DeviceBase<>).IsAssignableFrom(type.BaseType.GetGenericTypeDefinition())))
            {
                // Exception if already registered..
                _deviceTypes.Add(deviceType.Name, deviceType);
            }
        }

        private Type GetSinkTypeOfDeviceType(Type deviceType)
        {
            // Exception if already registered..
            return deviceType.BaseType.GetGenericArguments()[0];
        }

        private void LoadState(DeviceDescriptor[] descriptors)
        {
            if (descriptors != null)
            {
                foreach (var descriptor in descriptors)
                {
                    IDeviceInternal device = (IDeviceInternal)Activator.CreateInstance(_deviceTypes[descriptor.DeviceType]);
                    device.OnInitialize(descriptor.Argument, descriptor.SinkPath);
                    _devices.Add(device);
                }
            }
        }

        private void SaveState()
        {
            State = new Persistence
            {
                Descriptors = GetDeviceDescriptors()
            };
        }

        public IDevice CreateAndLoadDevice(string type, string argument, SinkPath sinkPath)
        {
            var device = (IDeviceInternal)Activator.CreateInstance(_deviceTypes[type]);
            _devices.Add(device);
            SaveState();

            device.OnInitialize(argument, sinkPath);
            return device;
        }

        public DeviceDescriptor[] GetDeviceDescriptors()
        {
            return _devices.Select(d => new DeviceDescriptor
            {
                SinkPath = d.SinkPath,
                DeviceType = d.GetType().Name,
                Argument = d.Argument
            }).ToArray();
        }
    }
}