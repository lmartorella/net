using Lucky.Home.Services;
using Lucky.Home.Sofar;

namespace Lucky.Home;

class Program
{
    static void Main(string[] args)
    {
        var manager = new Manager(args, "SolarConfiguration.json");
        manager.AddSingleton<MqttService>();
        manager.AddSingleton<Configuration>();
        manager.AddSingleton<SerializerFactory>();
        manager.AddSingleton<ModbusClientFactory>();
        manager.AddHostedService<PollStrategyManager>();
        manager.AddHostedService<Zcs6000TlmV3>();
        manager.Start();
    }
}
