using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Lucky.Home.Core;
using Lucky.Home.IO;
using Lucky.Home.Net;

namespace Lucky.Home.Admin
{
    class AdminListener : ServiceBase
    {
        private TcpListener _listener;
        public AdminListener()
        {
            var loopbackAddress = Dns.GetHostAddresses("localhost").FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            if (loopbackAddress == null)
            {
                Logger.Exception(new InvalidOperationException("Cannot find the loopback address of the host for admin connections"));
            }
            else
            {
                _listener = Manager.GetService<TcpService>().CreateListener(loopbackAddress, Constants.DefaultAdminPort, "Admin", HandleConnection);
            }
        }

        private async void HandleConnection(TcpClient client)
        {
            using (var stream = client.GetStream())
            {
                var channel = new MessageChannel(stream);
                var msg = await Receive<Container>(channel);
                if (msg.Message is GetTopologyMessage)
                {
                    // Returns the topology
                    var ret = new GetTopologyMessage.Response();
                    ret.Roots = BuildTree();
                    Send(channel, ret);
                }
            }
            client.Close();
        }

        private GetTopologyMessage.Node[] BuildTree()
        {
            return new[]
            {
                new GetTopologyMessage.Node {Children = new GetTopologyMessage.Node[0], Id = Guid.NewGuid()}
            };
        }

        private void Send<T>(MessageChannel stream, T message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new DataContractSerializer(message.GetType()).WriteObject(ms, message);
                ms.Flush();
                stream.WriteMessage(ms.GetBuffer());
            }
        }

        private async Task<T> Receive<T>(MessageChannel stream)
        {
            using (MemoryStream ms = new MemoryStream(await stream.ReadMessage()))
            {
                return (T)new DataContractSerializer(typeof(T)).ReadObject(ms);
            }
        }
    }
}
