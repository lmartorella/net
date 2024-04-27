using System.Runtime.Serialization;
using Lucky.Garden.Services;
using Microsoft.Extensions.Logging;

namespace Lucky.Garden.Device;

[DataContract]
public class Script
{
    [DataMember(Name = "id")]
    public int Id;

    [DataMember(Name = "name")]
    public string Name;
}

class ShellyScripts(ILogger<ShellyScripts> logger, Configuration configuration, RestService restService, SerializerFactory serializerFactory)
{
    [DataContract]
    private class ScriptListResp
    {
        [DataMember(Name = "scripts")]
        public Script[] Scripts;
    }
    
    [DataContract]
    private class ScriptGetCodeReq
    {
        [DataMember(Name = "id")]
        public int Id;
    }

    [DataContract]
    private class ScriptGetCodeResp
    {
        [DataMember(Name = "data")]
        public string Data;

        [DataMember(Name = "left")]
        public int Left;
    }

    [DataContract]
    private class ScriptPutCodeReq
    {
        [DataMember(Name = "id")]
        public int Id;

        [DataMember(Name = "code")]
        public string Code;

        [DataMember(Name = "append")]
        public bool Append;
    }

    [DataContract]
    private class ScriptPutCodeResp
    {
        [DataMember(Name = "len")]
        public int Len;
    }

    private string BaseUri
    {
        get
        {
            return $"{configuration.DeviceRest}/rpc/";
        }
    }

    public async Task<Script[]> GetScripts()
    {
        logger.LogInformation("FetchingScriptList");
        string json = await restService.AsyncRest(BaseUri + "Script.List", HttpMethod.Get);
        return serializerFactory.Create<ScriptListResp>().Deserialize(json).Scripts;
    }

    internal async Task<string> GetScriptCode(int id)
    {
        logger.LogInformation("FetchingConfCode: ID {0}", id);
        string json = await restService.AsyncRest(BaseUri + "Script.GetCode", HttpMethod.Get, serializerFactory.Create<ScriptGetCodeReq>().ToString(new ScriptGetCodeReq { Id = id }));
        var resp = serializerFactory.Create<ScriptGetCodeResp>().Deserialize(json);
        if (resp.Left > 0)
        {
            throw new InvalidOperationException("Script data truncated, to implement chunked read");
        }
        return resp.Data;
    }

    internal async Task SetScriptCode(int id, string code)
    {
        logger.LogInformation("SettingConfCode: ID {0}", id);
        string json = await restService.AsyncRest(BaseUri + "Script.PutCode", HttpMethod.Get, serializerFactory.Create<ScriptPutCodeReq>().ToString(new ScriptPutCodeReq { Id = id, Code = code, Append = false }));
        var resp = serializerFactory.Create<ScriptPutCodeResp>().Deserialize(json);
        if (resp.Len != code.Length)
        {
            throw new InvalidOperationException("Script length different from the set value");
        }
    }
}
