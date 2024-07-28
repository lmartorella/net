using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace Lucky.Home.Solar;

class CurrentSensorDevice(MqttService mqttService) : BackgroundService
{
    private double? lastHomeData;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // The ammeter uses will to send zero byte packet when disconnected
        await mqttService.SubscribeRawTopic(Constants.CurrentSensorHomeDataTopicId, async data =>
        {
            if (data == null || data.Length == 0)
            {
                LastHomeData = null;
            }
            else
            {
                LastHomeData = double.Parse(Encoding.UTF8.GetString(data));
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
    /// Event raised when new home usage data comes from the inverter
    /// </summary>
    public event EventHandler HomeDataChanged;

    /// <summary>
    /// Last sample of home usage. Null means offline
    /// </summary>
    public double? LastHomeData
    {
        get => lastHomeData;
        set
        {
            if (lastHomeData != value)
            {
                lastHomeData = value;
                HomeDataChanged?.Invoke(this, EventArgs.Empty);
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
