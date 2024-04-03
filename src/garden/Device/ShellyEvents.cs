using System.Runtime.Serialization;
using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Home.Device 
{
    /// <summary>
    /// Subscribe device events to .NET
    /// </summary>
    class ShellyEvents(ILogger<ShellyLogger> logger, Configuration configuration, MqttService mqttService) : BackgroundService
    {
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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
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
            // Never ending
            await new TaskCompletionSource().Task;
        }

        public event EventHandler<OutputEvent>? OutputChanged;
    }
}