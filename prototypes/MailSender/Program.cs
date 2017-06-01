using Lucky.Home.Notification;
using Lucky.Services;

namespace Lucky
{
    class Program
    {
        static void Main(string[] args)
        {
            Manager.Register<LoggerFactory, ILoggerFactory>();

            var notificationSvc = Manager.GetService<INotificationService>();
            notificationSvc.SendMail("Test Mail", "Ignore this message");
        }
    }
}
