using System;
using System.Threading.Tasks;
using Lucky.Home.Devices;
using Lucky.Services;

namespace Lucky.Home.Application
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public abstract class AppService : ServiceBase
    {
        private readonly TaskCompletionSource<object> _killDefer = new TaskCompletionSource<object>();

        public abstract Task Start();

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
            Console.CancelKeyPress += (sender, args) =>
            {
                Kill("detected CtrlBreak");
                args.Cancel = true;
            };
            await _killDefer.Task;

            // Safely stop devices
            await Manager.GetService<IDeviceManagerInternal>().TerminateAll();
            Logger.LogStderr("Exiting.");
        }

        public void Kill(string reason)
        {
            Logger.LogStderr("Server killing: + " + reason + ". Stopping devices...");
            _killDefer.TrySetResult(null);
        }
    }
}
