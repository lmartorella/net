using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Lucky.Home.Protocol;
using Lucky.IO;
using Lucky.Net;
using Lucky.Services;

namespace Lucky.Home.Admin
{
    class AdminListener : ServiceBase
    {
        private TcpListener _listener;
        private readonly INodeRegistrar _registrar;

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

            _registrar = Manager.GetService<NodeRegistrar>();
        }

        private async void HandleConnection(NetworkStream stream)
        {
            var channel = new MessageChannel(stream);
            var msg = await Receive<Container>(channel);
            if (msg.Message is GetTopologyMessage)
            {
                // Returns the topology
                var ret = new GetTopologyMessage.Response { Roots = BuildTree() };
                await Send(channel, ret);
            }
            else if (msg.Message is RenameNodeMessage)
            {
                var msg1 = (RenameNodeMessage)msg.Message;
                var node = _registrar.FindNode(msg1.Id);
                if (node != null)
                {
                    await node.Rename(msg1.NewId);
                }
            }
        }

        private Node[] BuildTree()
        {
            var roots = _registrar.Nodes.Where(n => !n.Address.IsSubNode).Select(n => new Node(n)).ToList();
            foreach (var node in _registrar.Nodes.Where(n => n.Address.IsSubNode))
            {
                var root = roots.FirstOrDefault(r => r.TcpNode.Address.Equals(node.Address.SubNode(0)));
                if (root != null)
                {
                    root.Children.Add(new Node(node));
                }
            }
            return roots.ToArray();
        }

        private async Task Send<T>(MessageChannel stream, T message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                new DataContractSerializer(message.GetType()).WriteObject(ms, message);
                ms.Flush();
                await stream.WriteMessage(ms.GetBuffer());
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
