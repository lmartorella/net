using System.Runtime.Serialization;
using Lucky.Garden.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Garden.Device;

class ShellyStatus(ILogger<ShellyStatus> logger, Configuration configuration, MqttService mqttService) : BackgroundService
{
    [DataContract]
    private class Status
    {
        [DataMember(Name = "mac")]
        public string Mac;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"Subscribing status");
        var topic = $"{configuration.DeviceId}/status/sys";
        await mqttService.SubscribeJsonTopic<Status>(topic, data => 
        {
            Online = data != null && data.Mac != null;
            if (data != null)
            {
                logger.LogInformation($"Status: Online. MAC address {data?.Mac}");
            }
            else
            {
                logger.LogInformation($"Status: Offline");
            }
        });
    }

    public bool Online;
}
