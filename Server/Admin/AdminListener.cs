using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using Lucky.Home.Core;
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

        private void HandleConnection(TcpClient client)
        {
            using (var stream = client.GetStream())
            {
                var msg = Receive<AdminMessage>(stream);
                if (msg is GetTopologyMessage)
                {
                    // Returns the topology
                    var ret = new GetTopologyMessage.Response();
                    ret.Roots = BuildTree();
                    Send(stream, ret);
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

        private void Send<T>(Stream stream, T message)
        {
            new DataContractSerializer(message.GetType()).WriteObject(stream, message);
            stream.Flush();
        }

        private T Receive<T>(Stream stream)
        {
            return (T)new DataContractSerializer(typeof(T)).ReadObject(stream);
        }
    }
}
