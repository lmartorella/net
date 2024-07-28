using Lucky.Home.Db;
using Lucky.Home.Services;
using Lucky.Home.Solar;

namespace Home.Solar;

class Program
{
    static int Main(string[] args)
    {
        var manager = new Manager(args);
        manager.AddSingleton<ResourceService>();
        manager.AddSingleton<MqttService>();
        manager.AddSingleton<SerializerFactory>();
        manager.AddHostedService<FsTimeSeries<PowerData, DayPowerData>>();
        manager.AddHostedService<InverterDevice>();
        manager.AddHostedService<CurrentSensorDevice>();
        manager.AddHostedService<NotificationSender>();
        manager.AddHostedService<NotificationService>();
        manager.AddHostedService<DataLogger>();
        manager.AddHostedService<UserInterface>();
        return manager.Start();
    }
}
