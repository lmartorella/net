using Lucky.Home.Notification;
using Lucky.Home.Services;

namespace Lucky.Home.Notification;

class Program
{
    static void Main(string[] args)
    {
        var manager = new Manager(args);
        manager.AddSingleton<MqttService>();
        manager.AddSingleton<Configuration>();
        manager.AddSingleton<SerializerFactory>();
        manager.Start();
    }
}
