using Lucky.Home.Notification;
using Lucky.Services;
using System;
using System.Threading;

namespace Lucky
{
    class Program
    {
        static void Main(string[] args)
        {
            Manager.Register<LoggerFactory, ILoggerFactory>();

            var notificationSvc = Manager.GetService<INotificationService>();
            notificationSvc.SendMail("Test Mail", "Ignore this message");

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
