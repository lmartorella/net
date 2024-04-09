using System.Runtime.Serialization;
using Lucky.Garden.Services;
using Microsoft.Extensions.Logging;

namespace Lucky.Garden.Device 
{
    [DataContract]
    public class Script
    {
        [DataMember(Name = "id")]
        public int Id;

        [DataMember(Name = "name")]
        public string Name;
    }

    public class ScriptWithCode : Script
    {
        public string Code;
    }

    class ShellyScrips(ILogger<ShellyScrips> logger, Configuration configuration, RestService restService, SerializerFactory serializerFactory)
    {
        [DataContract]
        private class ScriptListResp
        {
            [DataMember(Name = "scripts")]
            public Script[] Scripts;
        }
        
        private Uri BaseUri
        {
            get
            {
                return new Uri($"http://{configuration.DeviceName}/rpc/Script.List");
            }
        }

        public async Task<Script[]> GetScripts()
        {
            string json = await restService.AsyncRest(BaseUri, HttpMethod.Get);
            return serializerFactory.Create<ScriptListResp>().Deserialize(json).Scripts;
        }

        internal Task<ScriptWithCode> GetScript(int id)
        {
            throw new NotImplementedException();
        }
    }
}