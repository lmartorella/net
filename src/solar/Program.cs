using Lucky.Home.Db;
using Lucky.Home.Services;
using Lucky.Home.Solar;

namespace Home.Solar;

class Program
{
    static void Main(string[] args)
    {
        var manager = new Manager(args, "SolarConfiguration.json");
        manager.AddSingleton<ResourceService>();
        manager.AddSingleton<MqttService>();
        manager.AddSingleton<SerializerFactory>();
        manager.AddHostedService<FsTimeSeries<PowerData, DayPowerData>>();
        manager.AddHostedService<InverterDevice>();
        manager.AddHostedService<NotificationSender>();
        manager.AddHostedService<NotificationService>();
        manager.AddHostedService<DataLogger>();
        manager.AddHostedService<UserInterface>();
        manager.Start();
    }
}
