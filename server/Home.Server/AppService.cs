using System;
using System.Threading;
using System.Threading.Tasks;
using Lucky.Home.Devices;
using Lucky.Services;

namespace Lucky.Home.Application
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AppService : ServiceBase
    {
        public void Run()
        {
            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
                Logger.Exception((Exception)e.ExceptionObject);
            };

            StartLoop().Wait();
        }

        private async Task StartLoop()
        {
            var defer = new TaskCompletionSource<object>();
            Console.CancelKeyPress += (sender, args) =>
            {
                Logger.LogStderr("Detected CtrlBreak. Stopping devices...");
                defer.SetResult(null);
                args.Cancel = true;
            };
            await defer.Task;

            // Safely stop devices
            await Manager.GetService<DeviceManager>().TerminateAll();
            Logger.LogStderr("Exiting.");
        }
    }
}
