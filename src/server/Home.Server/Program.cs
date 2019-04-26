using System;
using Lucky.Home.Devices;
using Lucky.Home.Sinks;
using Lucky.Home.Services;
using Lucky.Home.Protocol;
using Lucky.Home.Admin;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace Lucky.Home.Lib
{
    static class Program
    {
        public static void Main(string[] arguments)
        {
            Run(arguments).Wait();
        }

        private static async Task Run(string[] arguments)
        {
            Manager.Register<JsonIsolatedStorageService, IIsolatedStorageService>();
            Manager.Register<NotificationService, INotificationService>();

            Manager.Register<LoggerFactory, ILoggerFactory>();
            Manager.Register<ConfigurationService, IConfigurationService>();
            Manager.GetService<ConfigurationService>().Init(arguments);

            LoggerFactory.Init(Manager.GetService<PersistenceService>());

            Manager.GetService<IIsolatedStorageService>().InitAppRoot("Server");

            Manager.Register<Server>();
            Manager.Register<NodeManager>();
            Manager.Register<SinkManager>();
            Manager.Register<SinkManager>();
            Manager.Register<DeviceManager, IDeviceManager>();
            Manager.GetService<SinkManager>().RegisterType(typeof(SystemSink));

            var logger = Manager.GetService<ILoggerFactory>().Create("Main");
            var applications = LibraryLoad(logger);

            Manager.GetService<DeviceManager>().Load();

            // Start server
            Manager.GetService<Server>();

            // Start Admin connection
            Manager.GetService<AdminListener>();

            foreach (var app in applications)
            {
                await app.Start();
            }

            Manager.GetService<SinkManager>().ResetSink += (o, e) =>
            {
                var systemSink = e.Sink.Node.Sink<ISystemSink>();
                if (systemSink != null)
                {
                    systemSink.Reset();
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
                logger.Exception((Exception)e.ExceptionObject);
            };

            Console.CancelKeyPress += (sender, args) =>
            {
                Manager.Kill(logger, "detected CtrlBreak");
                args.Cancel = true;
            };

            await Manager.Run();

            // Safely stop devices
            await Manager.GetService<DeviceManager>().TerminateAll();
            logger.LogStderr("Exiting.");
        }

        private static bool IsApplication(FileInfo fileInfo)
        {
            Assembly assembly = Assembly.ReflectionOnlyLoadFrom(fileInfo.FullName);
            return assembly.GetCustomAttributesData().Any(d => d.AttributeType.FullName == typeof(ApplicationAttribute).FullName);
        }

        private static IApplication[] LibraryLoad(ILogger logger)
        {
            ResolveEventHandler handler = (s, e) => Assembly.ReflectionOnlyLoad(e.Name);
            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += handler;

            // Find all .dlls in the bin folder and find for application
            var dlls = new FileInfo(Assembly.GetEntryAssembly().Location).Directory.GetFiles("*.dll").Where(dll => IsApplication(dll)).ToArray();
            List<IApplication> applications = new List<IApplication>();
            if (dlls.Length == 0)
            {
                logger.Warning("Application dll not found");
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
                    Manager.GetService<SinkManager>().RegisterAssembly(assembly);
                    // Register devices
                    Manager.GetService<DeviceManager>().RegisterAssembly(assembly);
                }
            }

            AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= handler;
            return applications.ToArray();
        }
    }
}
