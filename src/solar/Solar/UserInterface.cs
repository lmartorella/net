using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;

namespace Lucky.Home.Solar;

class UserInterface(MqttService mqttService, DataLogger dataLogger, InverterDevice inverterDevice) : BackgroundService
{
    /// <summary>
    /// This will never resets, and keep track of the last sampled grid voltage. Used even during night by home ammeter
    /// </summary>s
    private double _lastPanelVoltageV = -1.0;
    private NightState inverterNightState;

    public const string Topic = "ui/solar";
    public const string WillPayload = "null";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        inverterDevice.NewData += (o, e) => HandleNewInverterData(e);
        inverterDevice.NightStateChanged += (o, e) => UpdateInverterState(inverterDevice.NightState);
        UpdateInverterState(inverterDevice.NightState);
    }

    private void UpdateInverterState(NightState state)
    {
        inverterNightState = state;
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
            packet.InverterState = lastSample.InverterState.ToUserInterface(inverterNightState);

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
            if (inverterNightState == NightState.Running)
            {
                return OnlineStatus.Online;
            }
            if (inverterNightState != NightState.Running)
            {
                return OnlineStatus.Offline;
            }
            return OnlineStatus.PartiallyOnline;
        }
    }
}
