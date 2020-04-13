using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Services
{
    /// <summary>
    /// Generic based on pipe JSON message.
    /// Creates a Windows named bidirectional pipe for communication
    /// </summary>
    class PipeJsonServer<TReq, TResp>
    {
        private string _path;
        private DataContractJsonSerializer _reqSer;
        private DataContractJsonSerializer _respSer;
        private readonly List<Type> _reqAdditionalTypes = new List<Type>();
        private readonly List<Type> _respAdditionalTypes = new List<Type>();

        public PipeJsonServer(string path)
        {
            _path = path;
        }

        public void RegisterAdditionalRequestTypes(Type[] additionalTypes)
        {
            _reqAdditionalTypes.AddRange(additionalTypes);
            _reqSer = null;
        }

        public void RegisterAdditionalResponseTypes(Type[] additionalTypes)
        {
            _respAdditionalTypes.AddRange(additionalTypes);
            _respSer = null;
        }

        private DataContractJsonSerializer RequestSerializer
        {
            get
            {
                lock (this)
                {
                    return _reqSer ?? (_reqSer = new DataContractJsonSerializer(typeof(TReq), _reqAdditionalTypes));
                }
            }
        }

        private DataContractJsonSerializer ResponseSerializer
        {
            get
            {
                lock (this)
                {
                    return _respSer ?? (_respSer = new DataContractJsonSerializer(typeof(TResp), _respAdditionalTypes));
                }
            }
        }

        public Func<TReq, Task<Tuple<TResp, bool>>> ManageRequest { get; set; }

        public async Task Start()
        {
            bool quitLoop = false;
            while (!quitLoop)
            {
                NamedPipeServerStream stream = null;
                try
                {
                    // Open named pipe
                    stream = new NamedPipeServerStream(@"\\.\" + _path);
                    await stream.WaitForConnectionAsync();
                    TReq req;
                    {
                        var r = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
                        var buf = await r.ReadLineAsync();
                        req = (TReq)RequestSerializer.ReadObject(new MemoryStream(Encoding.UTF8.GetBytes(buf)));
                    }

                    var resp = await ManageRequest(req);
                    ResponseSerializer.WriteObject(stream, resp.Item1);
                    // New line
                    await stream.WriteAsync(new byte[] { 10, 13 }, 0, 2);
                    await stream.FlushAsync();
                    stream.WaitForPipeDrain();
                    stream.Disconnect();

                    quitLoop = resp.Item2;
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Dispose();
                    }
                }
            }
        }
    }
}
