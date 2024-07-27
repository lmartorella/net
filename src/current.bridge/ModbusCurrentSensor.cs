using Lucky.Home.Services;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Lucky.Home.Solar;

namespace Lucky.Home.Device;

class ModbusCurrentSensor(ILogger<ModbusCurrentSensor> logger, MqttService mqttService, ModbusClientFactory modbusClientFactory, Configuration configuration) : BackgroundService
{
    private int modbusNodeId;
    private ModbusClient modbusClient;
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(1.5);
    private double? lastData = double.MaxValue;
    private DeviceState? deviceState = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        modbusNodeId = configuration.AmmeterStationId;
        if (configuration.AmmeterHostName != "")
        {
            modbusClient = modbusClientFactory.Get(configuration.AmmeterHostName, FluentModbus.ModbusEndianness.BigEndian);
        }
        logger.LogInformation("Start: host {0}:{1}", configuration.AmmeterHostName, modbusNodeId);

        // Start wait loop
        while (true)
        {
            await Task.Delay(Period);
            await PullData();
        }
    }

    private async Task PullData()
    {
        // Check TCP MODBUS connection
        if (modbusClient == null || !modbusClient.CheckConnected())
        {
            await PublishData(null);
        }
        else
        {
            await PublishData(await GetData());
        }
    }

    private async Task<double?> GetData()
    {
        var buffer = await modbusClient.ReadHoldingRegistriesFloat(modbusNodeId, 0, 1);
        if (buffer.Length > 0)
        {
            return buffer[0];
        }
        else
        {
            return null;
        }
    }

    private async Task PublishData(double? data)
    {
        if (!data.HasValue && !lastData.HasValue)
        {
            return;
        }
        if (data.HasValue && lastData.HasValue && Math.Abs(data.Value - lastData.Value) < double.Epsilon)
        {
            return;
        }
        lastData = data;

        if (data != null)
        {
            await mqttService.RawPublish(Constants.CurrentSensorDataTopicId, Encoding.UTF8.GetBytes(data.ToString()));
            State = DeviceState.Online;
        }
        else
        {
            await mqttService.RawPublish(Constants.CurrentSensorDataTopicId, new byte[0]);
            State = DeviceState.Offline;
        }
    }

    public DeviceState? State
    {
        get => deviceState;
        set
        {
            if (!Equals(deviceState, value)) 
            {
                deviceState = value;
                _ = mqttService.RawPublish(Constants.CurrentSensorStateTopicId, Encoding.UTF8.GetBytes(value!.ToString()));
            }
        } 
    }
}
