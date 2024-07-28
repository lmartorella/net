using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;

namespace Lucky.Home.Solar;

class UserInterface(MqttService mqttService, DataLogger dataLogger, InverterDevice inverterDevice, CurrentSensorDevice currentSensorDevice) : BackgroundService
{
    /// <summary>
    /// This will never resets, and keep track of the last sampled grid voltage. Used even during night by home current sensor for
    /// a rough estimation of power
    /// </summary>s
    private double _lastPanelVoltageV = -1.0;
    private double? _lastHomeCurrentValue = null;

    public const string Topic = "ui/solar";
    public const string WillPayload = "null";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        inverterDevice.NewData += (o, e) => HandleNewInverterData(e);
        inverterDevice.DeviceStateChanged += (o, e) => UpdateInverterState();
        currentSensorDevice.DataChanged += (o, e) => UpdateHomeCurrentValue(currentSensorDevice.LastData.Home);
        UpdateInverterState();
    }

    private void UpdateInverterState()
    {
        PublishUpdate();
    }

    private void UpdateHomeCurrentValue(double? data)
    {
        _lastHomeCurrentValue = data;
        PublishUpdate();
    }

    private void HandleNewInverterData(PowerData data)
    {
        if (data.GridVoltageV > 0)
        {
            _lastPanelVoltageV = data.GridVoltageV;
        }
        PublishUpdate();
    }

    /// <summary>
    /// For the web GUI
    /// </summary>
    private void PublishUpdate()
    {
        var packet = new SolarRpcResponse
        {
            Status = OnlineStatus
        };
        var lastSample = dataLogger.GetLastSample();
        if (lastSample != null)
        {
            packet.CurrentW = lastSample.PowerW;
            packet.CurrentTs = lastSample.FromInvariantTime(lastSample.TimeStamp).ToString("F");
            packet.TotalDayWh = lastSample.EnergyTodayWh;
            packet.TotalKwh = lastSample.TotalEnergyKWh;
            packet.InverterState = lastSample.InverterState.ToUserInterface(inverterDevice.DeviceState);

            // From a recover boot 
            if (_lastPanelVoltageV <= 0 && lastSample.GridVoltageV > 0)
            {
                _lastPanelVoltageV = lastSample.GridVoltageV;
            }

            // Find the peak power
            var dayData = dataLogger.GetAggregatedData();
            if (dayData != null)
            {
                packet.PeakW = dayData.PeakPowerW;
                packet.PeakWTs = dayData.FromInvariantTime(dayData.PeakPowerTimestamp).ToString("hh\\:mm\\:ss");
                packet.PeakV = dayData.PeakVoltageV;
                packet.PeakVTs = dayData.FromInvariantTime(dayData.PeakVoltageTimestamp).ToString("hh\\:mm\\:ss");
            }
        }

        if (lastSample?.GridVoltageV > 0)
        {
            packet.GridV = lastSample.GridVoltageV;
            packet.UsageA = lastSample.HomeUsageCurrentA;
        }
        else if (_lastPanelVoltageV > 0)
        {
            // APPROX: Use last panel voltage with up-to-date home power usage
            packet.GridV = _lastPanelVoltageV;
            packet.UsageA = _lastHomeCurrentValue ?? -1.0;
        }
        else
        {
            packet.GridV = -1;
            packet.UsageA = -1.0;
        }

        mqttService.JsonPublish(Topic, packet);
    }

    private OnlineStatus OnlineStatus
    {
        get
        {
            // InverterState.ModbusConnecting means modbus server down. Other states means modbus up
            if (inverterDevice.DeviceState == DeviceState.Online && currentSensorDevice.DeviceState == DeviceState.Online)
            {
                return OnlineStatus.Online;
            }
            if (inverterDevice.DeviceState == DeviceState.Offline && currentSensorDevice.DeviceState == DeviceState.Offline)
            {
                return OnlineStatus.Offline;
            }
            return OnlineStatus.PartiallyOnline;
        }
    }
}
