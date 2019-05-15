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

            Manager.GetService<IIsolatedStorageService>().InitAppRoot("server");

            Manager.Register<Server>();
            Manager.Register<NodeManager>();
            Manager.Register<SinkManager>();
            Manager.Register<SinkManager>();
            Manager.Register<DeviceManager, IDeviceManager>();
            Manager.Register<SinkManager, ISinkManager>();
            Manager.GetService<SinkTypeManager>().RegisterType(typeof(SystemSink));
            Manager.GetService<DeviceTypeManager>();

            var logger = Manager.GetService<ILoggerFactory>().Create("Main");

            var registrar = Manager.GetService<Registrar>();
            var applications = new List<IApplication>();
            registrar.AssemblyLoaded += (o, e) =>
            {
                Type mainType = e.Item.GetCustomAttribute<ApplicationAttribute>().ApplicationType;
                if (!typeof(IService).IsAssignableFrom(mainType))
                {
                    throw new InvalidOperationException("The main application " + mainType.FullName + " is not a service");
                }
                applications.Add(Activator.CreateInstance(mainType) as IApplication);
            };
            registrar.LoadLibraries(new[] { typeof(ApplicationAttribute) });

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
    }
}
