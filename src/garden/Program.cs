using Lucky.Garden.Device;
using Lucky.Garden.Notification;
using Lucky.Garden.Services;

namespace Lucky.Garden;

class Program
{
    static void Main(string[] args)
    {
        var manager = new Manager(args);
        manager.AddSingleton<MqttService>();
        manager.AddSingleton<Device.Configuration>();
        manager.AddSingleton<Notification.Configuration>();
        manager.AddSingleton<SerializerFactory>();
        manager.AddSingleton<WillService, IMqttWillProvider>();
        manager.AddHostedService<ShellyLogger>();
        manager.AddHostedService<ShellyStatus>();
        manager.AddHostedService<ShellyEvents>();
        manager.AddSingleton<ShellyScripts>();
        manager.AddSingleton<RestService>();
        manager.AddHostedService<StatusService>();
        manager.AddHostedService<ConfigService>();
        manager.AddSingleton<NotificationService, INotificationService>();
        manager.AddHostedService<ActivityNotifier>();
        manager.Start();
    }
}
