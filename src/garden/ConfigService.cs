
using System.Runtime.Serialization;
using Lucky.Garden.Device;
using Lucky.Garden.Services;

namespace Lucky.Garden;

[DataContract]
public class ProgramConfig
{
    [DataMember(Name = "zones")]
    public string[] Zones;

    [DataMember(Name = "programCycles")]
    public ProgramCycle[] ProgramCycles;
}

///          name: string;
///          start: ISO-string;
///          startTime: HH:mm:ss;
///          suspended: boolean;
///          disabled: boolean;
///          minutes: number;
[DataContract]
public class ProgramCycle
{

}

class ConfigService
{
    private readonly ShellyScrips shellyScrips;
    private readonly SerializerFactory.TypeSerializer<ProgramConfig> programConfigSerializer;

    public ConfigService(ShellyScrips shellyScrips, SerializerFactory serializerFactory)
    {
        this.shellyScrips = shellyScrips;
        programConfigSerializer = serializerFactory.Create<ProgramConfig>();
    }

    public async Task<ProgramConfig?> GetConfig() 
    {
        var scripts = await shellyScrips.GetScripts();
        var configScript = scripts.FirstOrDefault(script => script.Name == "config");
        if (configScript != null)
        {
            var script = await shellyScrips.GetScript(configScript.Id);
            return programConfigSerializer.Deserialize(Uncomment(script.Code));
        }
        else
        {
            // No config stored
            return null;
        }
    }

    private string Uncomment(string code)
    {
        throw new NotImplementedException();
    }
}
