using System;
using Lucky.Home.Devices;
using Lucky.Home.Sinks;
using Lucky.Home.Services;
using Lucky.Home.Protocol;
using Lucky.Home.Admin;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;

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

            LibraryLoad("Home.Server.App.dll");

            Manager.GetService<DeviceManager>().Load();

            // Start server
            Manager.GetService<Server>();

            // Start Admin connection
            Manager.GetService<AdminListener>();

            var app = Manager.GetService<AppService>();
            await app.Start();

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
                app.Logger.Exception((Exception)e.ExceptionObject);
            };

            Console.CancelKeyPress += (sender, args) =>
            {
                app.Kill("detected CtrlBreak");
                args.Cancel = true;
            };

            await Manager.GetService<AppService>().Run();

            // Safely stop devices
            await Manager.GetService<DeviceManager>().TerminateAll();
            app.Logger.LogStderr("Exiting.");
        }

        private static void LibraryLoad(string path)
        {
            var appModule = Assembly.LoadFrom(path);
            var types = appModule.GetTypes();

            // Register application services
            types.Where(t => typeof(AppService).IsAssignableFrom(t)).ToList().ForEach(t => Manager.Register(t, typeof(AppService)));

            // Register app sinks
            Manager.GetService<SinkManager>().RegisterAssembly(appModule);
            // Register devices
            Manager.GetService<DeviceManager>().RegisterAssembly(appModule);
        }
    }
}
