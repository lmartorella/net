using System.Runtime.Serialization;
using Lucky.Garden.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Garden.Device 
{
    /// <summary>
    /// Subscribe device events to .NET
    /// </summary>
    class ShellyEvents
    {
        private readonly ILogger<ShellyLogger> logger;
        private readonly Configuration configuration;
        private readonly MqttService mqttService;

        public ShellyEvents(ILogger<ShellyLogger> logger, Configuration configuration, MqttService mqttService)
        {
            this.logger = logger;
            this.configuration = configuration;
            this.mqttService = mqttService;
            _ = Init();
        }
        
        [DataContract]
        public class OutputEvent
        {
            /// <summary>
            /// Output ID (0-2)
            /// </summary>
            [DataMember(Name = "id")]
            public int Id;

            [DataMember(Name = "output")]
            public bool Output;

            /// <summary>
            /// In seconds
            /// </summary>
            [DataMember(Name = "timer_duration")]
            public double TimerDuration = -1;
        }

        private async Task Init()
        {
            logger.LogInformation($"Subscribing switch events");
            for (int i = 0; i < 3; i++) {
                var topic = $"{configuration.DeviceId}/status/switch:{i}";
                await mqttService.SubscribeJsonTopic<OutputEvent>(topic, data => 
                {
                    if (data != null)
                    {
                        logger.LogInformation($"Output {data.Id} changed to {data.Output} for {data.TimerDuration} seconds");
                        OutputChanged?.Invoke(this, data);
                    }
                });
            }
        }

        public event EventHandler<OutputEvent>? OutputChanged;
    }
}