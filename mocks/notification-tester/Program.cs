using Lucky.Home.Services;

namespace Lucky.Tests;

class Program
{
    static void Main(string[] args)
    {
        var manager = new Manager(args);
        manager.AddSingleton<MqttService>();
        manager.AddSingleton<SerializerFactory>();
        manager.AddHostedService<SendTest>();
        manager.Start();
    }
}
