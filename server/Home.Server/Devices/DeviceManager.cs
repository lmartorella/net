using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Lucky.Services;

namespace Lucky.Home.Devices
{
    class DeviceManager : ServiceBaseWithData<DeviceManager.Persistence>, IDeviceManager
    {
        /// <summary>
        /// From type name to device type
        /// </summary>
        private readonly Dictionary<string, DeviceTypeDescriptor> _deviceTypes = new Dictionary<string, DeviceTypeDescriptor>();
        private readonly Dictionary<Guid, Tuple<DeviceBase, DeviceDescriptor>> _devices = new Dictionary<Guid, Tuple<DeviceBase, DeviceDescriptor>>();
        private List<Assembly> _assemblies = new List<Assembly>();
        public object DevicesLock { get; private set; }

        public DeviceManager()
        {
            DevicesLock = new object();
        }

        [DataContract]
        internal class Persistence
        {
            [DataMember]
            public DeviceDescriptor[] Descriptors { get; set; }
        }

        /// <summary>
        /// Used at exit
        /// </summary>
        internal async Task TerminateAll()
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

        public DeviceTypeDescriptor[] DeviceTypes
        {
            get
            {
                return _deviceTypes.Values.ToArray();
            }
        }

        public void RegisterAssembly(Assembly assembly)
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
            DeviceTypeDescriptor desc;
            if (!_deviceTypes.TryGetValue(descriptor.DeviceTypeName, out desc))
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

        public IEnumerable<IDevice> Devices
        {
            get
            {
                return _devices.Values.Select(v => v.Item1);
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