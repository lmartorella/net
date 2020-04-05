using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

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
    }

    [DataContract]
    public class WebResponse
    {
        [DataMember(Name = "error")]
        public string Error { get; set; }
    }

    /// <summary>
    /// The pipe communication for the web.server
    /// </summary>
    public class PipeServer : ServiceBase
    {
        public class MessageEventArgs : EventArgs
        {
            public WebRequest Request;
            public Task<WebResponse> Response;
            public bool CloseServer;
        }

        public event EventHandler<MessageEventArgs> Message;
        private PipeJsonServer<WebRequest, WebResponse> _server;

        public PipeServer()
        {
            _server = new PipeJsonServer<WebRequest, WebResponse>("NETHOME");
            _server.ManageRequest = async req =>
            {
                try
                {
                    var args = new MessageEventArgs() { Request = req, Response = Task.FromResult(new WebResponse()) };
                    Message?.Invoke(this, args);
                    var r = await args.Response;
                    return Tuple.Create(r, args.CloseServer);
                }
                catch (Exception exc)
                {
                    // Exc response
                    return Tuple.Create(new WebResponse { Error = exc.Message }, false);
                }
            };
            _ = _server.Start();
        }

        public void RegisterAdditionalRequestTypes(Type[] additionalTypes)
        {
            _server.RegisterAdditionalRequestTypes(additionalTypes);
        }

        public void RegisterAdditionalResponseTypes(Type[] additionalTypes)
        {
            _server.RegisterAdditionalResponseTypes(additionalTypes);
        }
    }
}
