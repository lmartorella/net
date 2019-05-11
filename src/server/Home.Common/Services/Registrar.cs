using Lucky.Home.Devices;
using Lucky.Home.Sinks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    class Registrar : ServiceBase
    {
        private static bool IsApplication(FileInfo fileInfo)
        {
            Assembly assembly = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName);
            return assembly.GetCustomAttributesData().Any(d => d.AttributeType.FullName == typeof(ApplicationAttribute).FullName);
        }

        public IApplication[] LoadLibraries()
        {
            ResolveEventHandler handler = (s, e) => Assembly.ReflectionOnlyLoad(e.Name);
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += handler;

            // Find all .dlls in the bin folder and find for application
            var dlls = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.GetFiles("*.dll").Where(dll => IsApplication(dll)).ToArray();
            List<IApplication> applications = new List<IApplication>();
            if (dlls.Length == 0)
            {
                Logger.Warning("Application dll not found");
            }
            else
            {
                foreach (FileInfo dll in dlls)
                {
                    // Load it in the AppDomain
                    Assembly assembly = Assembly.LoadFrom(dll.FullName);
                    Type mainType = assembly.GetCustomAttribute<ApplicationAttribute>().ApplicationType;
                    if (!typeof(IService).IsAssignableFrom(mainType))
                    {
                        throw new InvalidOperationException("The main application " + mainType.FullName + " is not a service");
                    }
                    applications.Add(Activator.CreateInstance(mainType) as IApplication);

                    var types = assembly.GetTypes();

                    // Register app sinks
                    Manager.GetService<SinkTypeManager>().RegisterAssembly(assembly);
                    // Register devices
                    Manager.GetService<DeviceTypeManager>().RegisterAssembly(assembly);
                }
            }

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= handler;
            return applications.ToArray();
        }
    }
}
