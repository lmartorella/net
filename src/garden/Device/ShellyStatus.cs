using System.Runtime.Serialization;
using Lucky.Garden.Services;
using Microsoft.Extensions.Logging;

namespace Lucky.Garden.Device 
{
    class ShellyStatus
    {
        private readonly ILogger<ShellyLogger> logger;
        private readonly Configuration configuration;
        private readonly MqttService mqttService;

        public ShellyStatus(ILogger<ShellyLogger> logger, Configuration configuration, MqttService mqttService)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.mqttService = mqttService;
            _ = Init();
        }

        [DataContract]
        private class Status
        {
            [DataMember(Name = "mac")]
            public string Mac;
        }
        
        private async Task Init()
        {
            logger.LogInformation($"Subscribing status");
            var topic = $"{configuration.DeviceId}/status/sys";
            await mqttService.SubscribeJsonTopic<Status>(topic, data => 
            {
                logger.LogInformation($"Status: MAC address {data?.Mac}");
                Online = data != null && data.Mac != null;
            });
        }

        public bool Online;
    }
}