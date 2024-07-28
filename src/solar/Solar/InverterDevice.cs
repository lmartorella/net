using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace Lucky.Home.Solar;

/// <summary>
/// Subscribe the inverter device's MQTT topics and exposes C# events for the loggers.
/// </summary>
class InverterDevice(MqttService mqttService) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Send inverter data to the data logger and notification server
        await mqttService.SubscribeJsonTopic(Constants.SolarDataTopicId, async (PowerData data) =>
        {
            NewData?.Invoke(this, data);
        });
        await mqttService.SubscribeRawTopic(Constants.SolarStateTopicId, async data =>
        {
            if (Enum.TryParse(Encoding.UTF8.GetString(data), out DeviceState))
            {
                DeviceStateChanged?.Invoke(this, DeviceState);
            }
        });
    }

    /// <summary>
    /// Event raised when new data comes from the inverter
    /// </summary>
    public event EventHandler<PowerData> NewData;

    /// <summary>
    /// The low-level inverter state
    /// </summary>
    public DeviceState DeviceState;

    /// <summary>
    /// Event raised when <see cref="DeviceState"/> changes.
    /// </summary>
    public event EventHandler<DeviceState> DeviceStateChanged;
}
