using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace Lucky.Home.Solar;

class CurrentSensorDevice(MqttService mqttService) : BackgroundService
{
    private double? lastData;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The ammeter uses will to send zero byte packet when disconnected
        await mqttService.SubscribeRawTopic(Constants.CurrentSensorDataTopicId, async data =>
        {
            if (data == null || data.Length == 0)
            {
                LastData = null;
            }
            else
            {
                LastData = double.Parse(Encoding.UTF8.GetString(data));
            }
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
    /// Event raised when new data comes from the inverter, or the state changes
    /// </summary>
    public event EventHandler DataChanged;

    /// <summary>
    /// Last sample. Null means offline
    /// </summary>
    public double? LastData
    {
        get => lastData;
        set
        {
            if (lastData != value)
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
