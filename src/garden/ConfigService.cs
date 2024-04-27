
using System.Runtime.Serialization;
using Lucky.Garden.Device;
using Lucky.Garden.Services;
using Microsoft.Extensions.Hosting;

namespace Lucky.Garden;

[DataContract]
public class ProgramConfig
{
    [DataMember(Name = "zones")]
    public string[] Zones;

    [DataMember(Name = "programCycles")]
    public ProgramCycle[] ProgramCycles;
}

[DataContract]
public class ProgramCycle
{
    [DataMember(Name = "name")]
    public string Name;

    [DataMember(Name = "start")]
    public string Start; // ISO

    [DataMember(Name = "startTime")]
    public string StartTime; // HH:mm:ss

    [DataMember(Name = "suspended")]
    public bool Suspended;

    [DataMember(Name = "disabled")]
    public bool Disabled;

    [DataMember(Name = "minutes")]
    public int Minutes;
}

class ConfigService : BackgroundService
{
    private readonly MqttService mqttService;
    private readonly ShellyScripts shellyScripts;
    private readonly SerializerFactory.TypeSerializer<ProgramConfig> programConfigSerializer;

    public ConfigService(MqttService mqttService, ShellyScripts shellyScripts, SerializerFactory serializerFactory)
    {
        this.mqttService = mqttService;
        this.shellyScripts = shellyScripts;
        programConfigSerializer = serializerFactory.Create<ProgramConfig>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await mqttService.SubscribeJsonRpc<RpcVoid, ProgramConfig>("garden/getConfiguration", (_) => GetConfig());
        await mqttService.SubscribeJsonRpc<ProgramConfig, RpcVoid>("garden/setConfiguration", SetConfig);
    }

    public async Task<ProgramConfig> GetConfig() 
    {
        throw new NotImplementedException();
        // var scripts = await shellyScripts.GetScripts();
        // var configScript = scripts.FirstOrDefault(script => script.Name == "config");
        // if (configScript != null)
        // {
        //     var script = await shellyScripts.GetScript(configScript.Id);
        //     return programConfigSerializer.Deserialize(Uncomment(script.Code))!;
        // }
        // else
        // {
        //     // No config stored. Return empty config
        //     return new ProgramConfig { Zones = [], ProgramCycles = [] };
        // }
    }

    private Task<RpcVoid> SetConfig(ProgramConfig? programConfig) 
    {
        throw new NotImplementedException();
    }

    private string Uncomment(string code)
    {
        throw new NotImplementedException();
    }

    private string Comment(string code)
    {
        throw new NotImplementedException();
    }
}
