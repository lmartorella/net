using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Lucky.Home.Services;

namespace Lucky.Home.Devices
{
    /// <summary>
    /// Device manager implementation
    /// </summary>
    class DeviceManager : ServiceBaseWithData<DeviceManager.Persistence>, IDeviceManager
    {
        private readonly Dictionary<Guid, Tuple<DeviceBase, DeviceDescriptor>> _devices = new Dictionary<Guid, Tuple<DeviceBase, DeviceDescriptor>>();

        [DataContract]
        internal class Persistence
        {
            [DataMember]
            public DeviceDescriptor[] Descriptors { get; set; }
        }

        /// <summary>
        /// Used at exit
        /// </summary>
        public async Task TerminateAll()
        {
            List<DeviceBase> devices = new List<DeviceBase>();
            lock (_devices)
            {
                devices = _devices.Values.Select(v => v.Item1).ToList();
            }

            foreach (var device in devices)
            {
                await device.OnTerminate();
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

        private DeviceBase CreateDevice(DeviceDescriptor descriptor)
        {
            var type = Manager.GetService<DeviceTypeManager>().GetDeviceType(descriptor.DeviceTypeName);
            if (type == null)
            {
                return null;
            }

            DeviceBase device = (DeviceBase)Activator.CreateInstance(type, FillDefaultArguments(descriptor.Arguments, type));
            device.OnInitialize(descriptor.SinkPaths);
            descriptor.Id = Guid.NewGuid();
            lock (_devices)
            {
                _devices.Add(descriptor.Id, Tuple.Create(device, descriptor));
                DevicesChanged?.Invoke(this, EventArgs.Empty);
            }
            Logger.Log("DeviceCreate", "Type", descriptor.DeviceTypeName);
            return device;
        }

        private static object[] FillDefaultArguments(object[] arguments, Type deviceType)
        {
            ConstructorInfo[] constructorInfos = deviceType.GetConstructors();
            // Uses the first constructor
            if (constructorInfos.Length != 1)
            {
                throw new InvalidOperationException("The device type " + deviceType.Name + " has too many or no public constructors");
            }
            var constructorInfo = constructorInfos[0];
            return constructorInfo.GetParameters().Select((arg, i) =>
            {
                if (i < arguments.Length)
                {
                    return arguments[i];
                }
                if (arg.DefaultValue != null)
                {
                    return arg.DefaultValue;
                }
                throw new InvalidOperationException("The device type " + deviceType.Name + " wants more mandatory arguments than registered");
            }).ToArray();
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

        public IDevice[] Devices
        {
            get
            {
                lock (_devices)
                {
                    return _devices.Values.Select(v => v.Item1).ToArray();
                }
            }
        }

        public event EventHandler DevicesChanged;

        public void Load()
        {
            Logger.Log("==== START ====");
            LoadState(State.Descriptors);
        }

        public async Task DeleteDevice(Guid id)
        {
            Tuple<DeviceBase, DeviceDescriptor> tuple = null;
            lock (_devices)
            {
                if (_devices.TryGetValue(id, out tuple))
                {
                    Logger.Log("Device Deleted", "Type", tuple.Item2.DeviceTypeName);
                    _devices.Remove(id);
                }
            }
            if (tuple != null)
            {
                SaveState();
                DevicesChanged?.Invoke(this, EventArgs.Empty);
                await tuple.Item1.OnTerminate();
            }
        }
    }
}