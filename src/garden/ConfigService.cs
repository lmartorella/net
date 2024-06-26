
using System.Runtime.Serialization;
using Lucky.Garden.Device;
using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;

namespace Lucky.Garden;

[DataContract]
public class ProgramConfig
{
    /// <summary>
    /// Names of the relay outputs
    /// </summary>
    [DataMember(Name = "names")]
    public string[] ZoneNames;

    /// <summary>
    /// Cycles
    /// </summary>
    [DataMember(Name = "programCycles")]
    public ProgramCycle[] ProgramCycles;

    /// <summary>
    /// Globally suspended
    /// </summary>
    [DataMember(Name = "suspended")]
    public bool Suspended;
}

[DataContract]
public class ProgramCycle
{
    /// <summary>
    /// Zone to activate
    /// </summary>
    [DataMember(Name = "name")]
    public string ZoneName;

    /// <summary>
    /// Start time
    /// </summary>
    [DataMember(Name = "startTime")]
    public string StartTime; // HH:mm:ss

    /// <summary>
    /// Repeat every number of days (1 means daily, 2 every 2 days, etc...), 0 is invalid
    /// </summary>
    [DataMember(Name = "everyDays")]
    public int EveryDays;

    /// <summary>
    /// On/off
    /// </summary>
    [DataMember(Name = "disabled")]
    public bool Disabled;

    /// <summary>
    /// Duration in minutes
    /// </summary>
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
        programConfigSerializer = serializerFactory.Create<ProgramConfig>(true);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await mqttService.SubscribeJsonRpc<RpcVoid, ProgramConfig>("garden/getConfiguration", (_) => GetConfig());
        await mqttService.SubscribeJsonRpc<ProgramConfig, RpcVoid>("garden/setConfiguration", SetConfig);
    }

    public async Task<ProgramConfig> GetConfig() 
    {
        var scripts = await shellyScripts.GetScripts();
        var configScript = scripts.FirstOrDefault(script => script.Name == "config");
        if (configScript != null)
        {
            var script = await shellyScripts.GetScriptCode(configScript.Id);
            return programConfigSerializer.Deserialize(Uncomment(script))!;
        }
        else
        {
            // No config stored. Return empty config
            return new ProgramConfig { ZoneNames = [], ProgramCycles = [] };
        }
    }

    private async Task<RpcVoid> SetConfig(ProgramConfig? programConfig) 
    {
        string code = Comment(programConfigSerializer.ToString(programConfig)!);

        var scripts = await shellyScripts.GetScripts();
        var configScript = scripts.FirstOrDefault(script => script.Name == "config");
        int id;
        if (configScript != null)
        {
            id = configScript.Id;
        }
        else
        {
            id = await shellyScripts.CreateScript("config");
        }
        await shellyScripts.SetScriptCode(id, code);
        return new RpcVoid();
    }

    private string Uncomment(string code)
    {
        return code.Replace("*/", "").Replace("/*", "");
    }

    private string Comment(string code)
    {
        return "/*" + Environment.NewLine + code + Environment.NewLine + "*/";
    }
}
