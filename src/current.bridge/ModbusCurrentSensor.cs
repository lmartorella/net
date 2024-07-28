using Lucky.Home.Services;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModbusClient = Lucky.Home.Services.ModbusClient;
using FluentModbus;
using System.Runtime.Serialization;

namespace Lucky.Home.Solar;

[DataContract]
public class CurrentSensorData
{
    [DataMember(Name = "home")]
    public double Home;

    [DataMember(Name = "export")]
    public double Export;
}

class ModbusCurrentSensor(ILogger<ModbusCurrentSensor> logger, MqttService mqttService, ModbusClientFactory modbusClientFactory, Configuration configuration) : BackgroundService
{
    private int modbusNodeId;
    private ModbusClient modbusClient;
    private static readonly TimeSpan Period = TimeSpan.FromSeconds(1.5);
    private DeviceState? deviceState = null;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        modbusNodeId = configuration.CurrentSensorStationId;
        if (configuration.CurrentSensorHostName != "")
        {
            modbusClient = modbusClientFactory.Get(configuration.CurrentSensorHostName, ModbusEndianness.LittleEndian);
        }
        logger.LogInformation("Start: host {0}:{1}", configuration.CurrentSensorHostName, modbusNodeId);

        // Start wait loop
        while (true)
        {
            await Task.Delay(Period);
            // Check TCP MODBUS connection
            if (modbusClient == null || !modbusClient.CheckConnected())
            {
                // Put state offline as well
                await PublishData(null);
            }
            else
            {
                // If GetData() fails, propagate the Offline state
                await PublishData(await GetData());
            }
        }
    }

    /// <summary>
    /// Get both channel values, as ampere
    /// </summary>
    private async Task<float[]> GetData()
    {
        // PIC current sensor specs, little endian fixed point 16+16
        var buffer = await modbusClient.ReadHoldingRegistries<ushort>(modbusNodeId, 0x200, 4);
        if (buffer == null)
        {
            return null;
        }
        float[] ret = new float[2];
        for (int i = 0; i < 2; i++)
        {
            float value = ((uint)(buffer[i * 2] + (buffer[i * 2 + 1] << 16))) / 65536f;
            // full-scale = 1024, 50A sensor
            ret[i] = value / 1024f * 50f;
        }                        
        return ret;
    }

    private async Task PublishData(float[] data)
    {
        if (data == null && State == DeviceState.Offline)
        {
            return;
        }
        if (data != null)
        {
            await mqttService.JsonPublish(Constants.CurrentSensorDataTopicId, new CurrentSensorData
            {
                Home = data[0],
                Export = data[1]
            });
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
                logger.LogInformation("DeviceState changed to {0}", value);
                deviceState = value;
                _ = mqttService.RawPublish(Constants.CurrentSensorStateTopicId, Encoding.UTF8.GetBytes(value!.ToString()));
            }
        } 
    }
}
