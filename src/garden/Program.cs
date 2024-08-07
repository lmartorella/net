﻿using Lucky.Garden.Device;
using Lucky.Home.Services;

namespace Lucky.Home.Garden;

class Program
{
    static int Main(string[] args)
    {
        var manager = new Manager(args, "gardenConfiguration.json");
        manager.AddSingleton<ResourceService>();
        manager.AddSingleton<MqttService>();
        manager.AddSingleton<Configuration>();
        manager.AddSingleton<SerializerFactory>();
        manager.AddSingleton<WillService, IMqttWillProvider>();
        manager.AddHostedService<ShellyLogger>();
        manager.AddHostedService<ShellyStatus>();
        manager.AddHostedService<ShellyEvents>();
        manager.AddSingleton<ShellyScripts>();
        manager.AddSingleton<RestService>();
        manager.AddHostedService<StatusService>();
        manager.AddHostedService<ConfigService>();
        manager.AddHostedService<ActivityNotifier>();
        return manager.Start();
    }
}
