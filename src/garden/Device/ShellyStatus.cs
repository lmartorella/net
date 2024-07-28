using System.Runtime.Serialization;
using System.Text;
using Lucky.Home;
using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Garden.Device;

class ShellyStatus(ILogger<ShellyStatus> logger, Configuration configuration, MqttService mqttService) : BackgroundService
{
    private DeviceState state = DeviceState.Offline;

    [DataContract]
    private class Status
    {
        [DataMember(Name = "mac")]
        public string Mac;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Subscribing status");
        await mqttService.SubscribeJsonTopic<Status>($"{configuration.DeviceId}/status/sys", async data => 
        {
            if (data != null)
            {
                logger.LogInformation($"Status: Online. MAC address {data?.Mac}");
            }
            else
            {
                logger.LogInformation($"Status: Offline");
            }
        });
        await mqttService.SubscribeRawTopic($"{configuration.DeviceId}/online", async data => 
        {
            State = Encoding.UTF8.GetString(data) == "false" ? DeviceState.Offline : DeviceState.Online;
        });
    }

    public DeviceState State
    {
        get => state;
        set
        {
            if (state != value)
            {
                state = value;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public event EventHandler StateChanged;
}
