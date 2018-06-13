using Lucky.Home.Model;
using Lucky.Net;
using Lucky.Services;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using static Lucky.Home.Devices.GardenDevice;

namespace Lucky.Home.Services
{
    public class PipeServer : ServiceBase
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
        public class WebResponse
        {
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
            /// Status
            /// </summary>
            [DataMember(Name = "status")]
            public string Status{ get; set; }
        }

        public class MessageEventArgs : EventArgs
        {
            public WebRequest Request;
            public WebResponse Response;
        }

        public event EventHandler<MessageEventArgs> Message;

        public PipeServer()
        {
            var server = new PipeJsonServer<WebRequest, WebResponse>("NETHOME");
            server.ManageRequest = req =>
            {
                var args = new MessageEventArgs() { Request = req, Response = new WebResponse() };
                Message?.Invoke(this, args);
                return Task.FromResult(args.Response);
            };
            server.Start();
        }
    }
}
