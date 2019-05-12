using Lucky.Home.Devices;
using Lucky.Home.Sinks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Lucky.Home.Services
{
    class Registrar : ServiceBase
    {
        private static bool IsApplication(FileInfo fileInfo, IEnumerable<string> allowedLibraryAttributeNames)
        {
            Assembly assembly = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName);
            return assembly.GetCustomAttributesData().Any(d => allowedLibraryAttributeNames.Contains(d.AttributeType.FullName));
        }

        public void LoadLibraries(Type[] allowedLibraryAttributes)
        {
            ResolveEventHandler handler = (s, e) => Assembly.ReflectionOnlyLoad(e.Name);
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += handler;

            // Find all .dlls in the bin folder and find for application
            var dlls = new FileInfo(Assembly.GetEntryAssembly().Location)
                .Directory
                .GetFiles("*.dll")
                .Where(dll => IsApplication(dll, allowedLibraryAttributes.Select(t => t.FullName)))
                .ToArray();
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

                    AssemblyLoaded?.Invoke(this, new ItemEventArgs<Assembly>(assembly));
                }
            }

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= handler;
        }

        public event EventHandler<ItemEventArgs<Assembly>> AssemblyLoaded;
    }
}
