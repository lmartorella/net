
using System.Runtime.Serialization;
using Lucky.Garden.Device;
using Lucky.Garden.Services;
using Microsoft.Extensions.Hosting;

namespace Lucky.Garden;

[DataContract]
public class StatusType
{
    [DataMember(Name = "error")]
    public string Error;

    [DataMember(Name = "status")]
    public StatusCode StatusCode;

    // [DataMember(Name = "isRunning")]
    // public bool isRunning;

    [DataMember(Name = "config")]
    public ProgramConfig? Config;
}

public enum StatusCode
{
    Online = 1,
    Offline = 2,
    PartiallyOnline = 3
}

class StatusService(MqttService mqttService, ConfigService configService, ShellyStatus shellyStatus) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await mqttService.SubscribeJsonRpc<RpcVoid, StatusType>("garden/getStatus", (_) => GetStatus());
    }

    private async Task<StatusType> GetStatus()
    {
        try
        {
            return new StatusType
            {
                StatusCode = shellyStatus.Online ? StatusCode.Online : StatusCode.Offline,
                Config = await configService.GetConfig()
            };
        }
        catch (Exception exc)
        {
            return new StatusType
            {
                Error = exc.Message
            };
        }
    }
}
