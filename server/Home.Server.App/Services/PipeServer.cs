using Lucky.Home.Sinks;
using Lucky.Net;
using Lucky.Services;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using static Lucky.Home.Devices.GardenDevice;

namespace Lucky.Home.Services
{
    [DataContract]
    public class WebRequest
    {
        /// <summary>
        /// Can be getProgram, setImmediate
        /// </summary>
        [DataMember(Name = "command")]
        public string Command { get; set; }

        [DataMember(Name = "immediate")]
        public int[] ImmediateZones { get; set; }
    }

    [DataContract]
    [KnownType(typeof(GardenWebResponse))]
    public class WebResponse
    {
        public bool CloseServer;
    }

    [DataContract]
    public class GardenWebResponse : WebResponse
    {
        /// <summary>
        /// Status
        /// </summary>
        [DataMember(Name = "status")]
        public string Status { get; set; }

        /// <summary>
        /// Result/error
        /// </summary>
        [DataMember(Name = "error")]
        public string Error { get; set; }

        /// <summary>
        /// Configuration
        /// </summary>
        [DataMember(Name = "config")]
        public Configuration Configuration { get; set; }

        /// <summary>
        /// For gardem
        /// </summary>
        [DataMember(Name = "online")]
        public bool Online { get; set; }

        /// <summary>
        /// For gardem
        /// </summary>
        [DataMember(Name = "flowData")]
        public FlowData FlowData { get; set; }
    }

    public class PipeServer : ServiceBase
    {
        public class MessageEventArgs : EventArgs
        {
            public WebRequest Request;
            public Task<WebResponse> Response;
        }

        public event EventHandler<MessageEventArgs> Message;

        public PipeServer()
        {
            var server = new PipeJsonServer<WebRequest, WebResponse>("NETHOME");
            server.ManageRequest = async req =>
            {
                var args = new MessageEventArgs() { Request = req, Response = Task.FromResult(new WebResponse()) };
                Message?.Invoke(this, args);
                var r = await args.Response;
                return Tuple.Create(r, r.CloseServer);
            };
            server.Start();
        }
    }
}
