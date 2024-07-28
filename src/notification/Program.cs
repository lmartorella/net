using Lucky.Home.Services;

namespace Lucky.Home.Notification;

class Program
{
    static int Main(string[] args)
    {
        var manager = new Manager(args, "notification.json");
        manager.AddSingleton<MqttService>();
        manager.AddSingleton<Configuration>();
        manager.AddSingleton<SerializerFactory>();
        manager.AddHostedService<NotificationService>();
        return manager.Start();
    }
}
