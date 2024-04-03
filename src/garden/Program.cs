using Lucky.Home.Device;
using Lucky.Home.Services;

namespace Lucky.Net;

class Program
{
    static void Main(string[] args)
    {
        var manager = new Manager(args);
        manager.AddSingleton<MqttService>();
        manager.AddSingleton<Configuration>();
        manager.AddHostedService<ShellyLogger>();
        manager.AddHostedService<ShellyEvents>();
        manager.Start();
    }
}
