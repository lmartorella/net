using System.Text;
using Lucky.Home.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Home 
{
    /// <summary>
    /// Store sys log of Shelly device to a .log file. Supports rotation
    /// </summary>
    class ShellyLogger(ILogger<ShellyLogger> logger, IConfiguration configuration, MqttService mqttService) : BackgroundService
    {
        private string DeviceId
        {
            get
            {
                return configuration["deviceId"] ?? "garden-device";
            }
        }

        private FileInfo LogFilePath
        {
            get
            {
                return new FileInfo(Path.Join(Environment.CurrentDirectory, "garden-device.log"));
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var topic = $"{DeviceId}/debug/log";
            logger.LogInformation($"Subscribing {topic}");
            await mqttService.SubscribeRawTopic(topic, data => 
            {
                using (StreamWriter writer = LogFilePath.AppendText())
                {
                    writer.WriteLine(Encoding.UTF8.GetString(data));
                }
            });
            logger.LogInformation($"Subscribed {topic}");
            // Never ending
            await new TaskCompletionSource().Task;
        }
    }
}