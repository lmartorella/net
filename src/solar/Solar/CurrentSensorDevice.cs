using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;
using System.Runtime.Serialization;
using System.Text;

namespace Lucky.Home.Solar;

[DataContract]
public class CurrentSensorData
{
    [DataMember(Name = "home")]
    public double Home;

    [DataMember(Name = "export")]
    public double Export;
}

class CurrentSensorDevice(MqttService mqttService) : BackgroundService
{
    private CurrentSensorData? lastData;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The ammeter uses will to send zero byte packet when disconnected
        await mqttService.SubscribeJsonTopic<CurrentSensorData>(Constants.CurrentSensorDataTopicId, async data =>
        {
            LastData = data;
        });
        await mqttService.SubscribeRawTopic(Constants.CurrentSensorStateTopicId, async data => 
        {
            if (Enum.TryParse(Encoding.UTF8.GetString(data), out DeviceState))
            {
                DeviceStateChanged?.Invoke(this, DeviceState);
            }
        });
    }

    /// <summary>
    /// Event raised when new home usage data comes from the inverter
    /// </summary>
    public event EventHandler DataChanged;

    /// <summary>
    /// Last sample of home usage. Null means offline
    /// </summary>
    public CurrentSensorData? LastData
    {
        get => lastData;
        set
        {
            if (!Equals(lastData, value))
            {
                lastData = value;
                DataChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// The low-level inverter state
    /// </summary>
    public DeviceState DeviceState;

    /// <summary>
    /// Event raised when <see cref="DeviceState"/> changes.
    /// </summary>
    public event EventHandler<DeviceState> DeviceStateChanged;
}
