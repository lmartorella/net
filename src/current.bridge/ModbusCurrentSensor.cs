using Lucky.Home.Services;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModbusClient = Lucky.Home.Services.ModbusClient;
using FluentModbus;

namespace Lucky.Home.Solar;

class ModbusCurrentSensor(ILogger<ModbusCurrentSensor> logger, MqttService mqttService, ModbusClientFactory modbusClientFactory, Configuration configuration) : BackgroundService
{
    private int modbusNodeId;
    private ModbusClient modbusClient;
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(1.5);
    private DeviceState? deviceState = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        modbusNodeId = configuration.AmmeterStationId;
        if (configuration.AmmeterHostName != "")
        {
            modbusClient = modbusClientFactory.Get(configuration.AmmeterHostName, FluentModbus.ModbusEndianness.LittleEndian);
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

    /// <summary>
    /// Get both channel values, as ampere
    /// </summary>
    private async Task<float[]> GetData()
    {
        try
        {
            // PIC ammeter specs, little endian
            var buffer = await modbusClient.ReadHoldingRegistries(modbusNodeId, 0x200, 4);
            float[] ret = new float[2];
            for (int i = 0; i < 2; i++)
            {
                float value = ((uint)(buffer[i * 2] + (buffer[i * 2 + 1] << 16))) / 65536f;
                // full-scale = 1024, 50A sensor
                ret[i] = value / 1024f * 50f;
            }                        
            return ret;
        }
        catch (ModbusException exc)
        {
            if (exc.ExceptionCode != ModbusExceptionCode.GatewayTargetDeviceFailedToRespond)
            {
                logger.LogError(exc, "ModbusExc");
            }
            // The bridge RTU-to-TCP responded with some error that is not managed, so it is alive
            // Even the RTU timeout is managed by the gateway and translated to a modbus error
            return null;
        }
    }

    private async Task PublishData(float[] data)
    {
        if (data == null && State == DeviceState.Offline)
        {
            return;
        }
        if (data != null)
        {
            await mqttService.RawPublish(Constants.CurrentSensorHomeDataTopicId, Encoding.UTF8.GetBytes(data[0].ToString()));
            await mqttService.RawPublish(Constants.CurrentSensorExportDataTopicId, Encoding.UTF8.GetBytes(data[1].ToString()));
            State = DeviceState.Online;
        }
        else
        {
            await mqttService.RawPublish(Constants.CurrentSensorHomeDataTopicId, new byte[0]);
            await mqttService.RawPublish(Constants.CurrentSensorExportDataTopicId, new byte[0]);
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
                logger.LogInformation("DeviceState changed to {0}", value);
                deviceState = value;
                _ = mqttService.RawPublish(Constants.CurrentSensorStateTopicId, Encoding.UTF8.GetBytes(value!.ToString()));
            }
        } 
    }
}
