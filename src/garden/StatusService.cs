
using System.Runtime.Serialization;
using Lucky.Garden.Device;
using Lucky.Garden.Services;

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
    //PartiallyOnline = 3
}

class StatusService
{
    private readonly MqttService mqttService;
    private readonly ConfigService configService;
    private readonly ShellyStatus shellyStatus;

    public StatusService(MqttService mqttService, ConfigService configService, ShellyStatus shellyStatus)
    {
        this.mqttService = mqttService;
        this.configService = configService;
        this.shellyStatus = shellyStatus;
        _ = Init();
    }

    private async Task Init()
    {
        await mqttService.JsonPublish("garden/status", await GetStatus());
    }

    public async Task<StatusType> GetStatus()
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
