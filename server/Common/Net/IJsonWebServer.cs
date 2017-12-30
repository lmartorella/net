using System;
using System.Threading.Tasks;

namespace Lucky.Net
{
    /// <summary>
    /// Interface for a JSON based web server (type serialized via DataContractJsonSerializer
    /// </summary>
    public interface IJsonWebServer<Treq, TResp>
    {
        Task Start();
        Func<Treq, Task<TResp>> ManageRequest { get; set; }
    }
}
