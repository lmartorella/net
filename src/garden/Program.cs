using Lucky.Home;
using Lucky.Home.Services;

namespace Lucky.Net;

class Program
{
    static void Main(string[] args)
    {
        var manager = new Manager(args);
        manager.Register<MqttService>();
        manager.RegisterHostedService<ShellyLogger>();
        manager.Start();
    }
}
