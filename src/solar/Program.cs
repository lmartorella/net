using Lucky.Home.Db;
using Lucky.Home.Services;
using Lucky.Home.Solar;
using Microsoft.Extensions.Logging;

namespace Home.Solar;

class Program
{
    private const string DeviceHostName = "localhost";

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
