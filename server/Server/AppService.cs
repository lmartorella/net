using System;
using System.Threading;
using Lucky.Services;

namespace Lucky.Home.Application
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AppService : ServiceBase
    {
        public virtual void Run()
        {
            AppDomain.CurrentDomain.UnhandledException += (o, e) =>
            {
                Logger.Exception((Exception)e.ExceptionObject);
            };

            WaitBreak();
        }

        private static void WaitBreak()
        {
            object lockObject = new object();
            Console.CancelKeyPress += (sender, args) =>
            {
                lock (lockObject)
                {
                    Monitor.Pulse(lockObject);
                }
            };
            lock (lockObject)
            {
                Monitor.Wait(lockObject);
            }
        }
    }
}
