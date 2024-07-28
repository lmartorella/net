using System.Runtime.Serialization;
using Lucky.Garden.Device;
using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;

namespace Lucky.Home.Garden;

[DataContract]
public class StatusType
{
    [DataMember(Name = "status")]
    public OnlineStatus OnlineStatus;
}

// [DataContract]
// public class StatusType
// {
//     [DataMember(Name = "error")]
//     public string Error;

//     [DataMember(Name = "isRunning")]
//     public bool isRunning;

//     [DataMember(Name = "config")]
//     public ProgramConfig? Config;
// }

/// <summary>
/// Get/set the current garden timer configuration
/// </summary>
class StatusService(MqttService mqttService, ConfigService configService, ShellyStatus shellyStatus, SerializerFactory serializerFactory) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        shellyStatus.StateChanged += (o, e) =>
        {
            UpdateState(shellyStatus.State);
        };
        UpdateState(shellyStatus.State);
        await mqttService.SubscribeJsonRpc<RpcVoid, ProgramConfig>("garden/getConfiguration", (_) => configService.GetConfig());
    }

    private async Task UpdateState(DeviceState state)
    {
        await mqttService.JsonPublish("ui/garden/state", new StatusType
        {
            OnlineStatus = state == DeviceState.Online ? OnlineStatus.Online : OnlineStatus.Offline
        });
    }
}
