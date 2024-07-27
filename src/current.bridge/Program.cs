using Lucky.Home.Services;
using Lucky.Home.Solar;

namespace Lucky.Home;

class Program
{
    static int Main(string[] args)
    {
        var manager = new Manager(args, "SolarConfiguration.json");
        manager.AddSingleton<MqttService>();
        manager.AddSingleton<WillService, IMqttWillProvider>();
        manager.AddSingleton<Configuration>();
        manager.AddSingleton<SerializerFactory>();
        manager.AddSingleton<ModbusClientFactory>();
        manager.AddHostedService<ModbusCurrentSensor>();
        return manager.Start();
    }
}
