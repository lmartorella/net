using Lucky.Garden.Device;
using Lucky.Garden.Services;

namespace Lucky.Garden;

class Program
{
    static void Main(string[] args)
    {
        var manager = new Manager(args);
        manager.AddSingleton<MqttService>();
        manager.AddSingleton<Configuration>();
        manager.AddSingleton<SerializerFactory>();
        manager.AddSingleton<WillService, IMqttWillProvider>();
        manager.AddHostedService<ShellyLogger>();
        manager.AddHostedService<ShellyStatus>();
        manager.Start();
    }
}
