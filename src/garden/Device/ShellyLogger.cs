using System.Text;
using Lucky.Garden.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Garden.Device;

/// <summary>
/// Store sys log of Shelly device to a .log file. TODO: supports rotation
/// </summary>
class ShellyLogger(ILogger<ShellyLogger> logger, Configuration configuration, MqttService mqttService) : BackgroundService
{
    private FileInfo LogFilePath
    {
        get
        {
            return new FileInfo(Path.Join(Environment.CurrentDirectory, "garden-device.log"));
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topic = $"{configuration.DeviceId}/debug/log";
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
