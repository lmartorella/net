using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Lucky.Home.Devices;
using Lucky.Home.Protocol;
using Lucky.IO;
using Lucky.Net;
using Lucky.Services;

namespace Lucky.Home.Admin
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class AdminListener : ServiceBase
    {
        private TcpListener _listener;
        private readonly INodeManager _manager;

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

            _manager = Manager.GetService<NodeManager>();
        }

        private async void HandleConnection(NetworkStream stream)
        {
            var channel = new MessageChannel(stream);
            while (true)
            {
                var msg = await Receive<Container>(channel);
                if (msg == null)
                {
                    // EOF
                    break;
                }

                if (msg.Message is GetTopologyMessage)
                {
                    // Returns the topology
                    var ret = new GetTopologyMessage.Response { Roots = BuildTree() };
                    await Send(channel, ret);
                }
                else if (msg.Message is RenameNodeMessage)
                {
                    var msg1 = (RenameNodeMessage)msg.Message;
                    ITcpNode node;
                    if (msg1.Id == Guid.Empty)
                    {
                        node = _manager.FindNode(TcpNodeAddress.Parse(msg1.NodeAddress));
                    }
                    else
                    {
                        node = _manager.FindNode(msg1.Id);
                    }
                    if (node != null)
                    {
                        await node.Rename(msg1.NewId);
                    }
                    var ret = new RenameNodeMessage.Response();
                    await Send(channel, ret);
                }
                else if (msg.Message is GetDeviceTypesMessage)
                {
                    var ret = new GetDeviceTypesMessage.Response { DeviceTypes = Manager.GetService<DeviceManager>().DeviceTypes };
                    await Send(channel, ret);
                }
                else if (msg.Message is CreateDeviceMessage)
                {
                    var msg1 = (CreateDeviceMessage)msg.Message;
                    string err = null;
                    try
                    {
                        CreateDevice(msg1.SinkPath, msg1.DeviceType, msg1.Argument);
                    }
                    catch (Exception exc)
                    {
                        err = exc.Message;
                    }
                    var ret = new CreateDeviceMessage.Response { Error = err };
                    await Send(channel, ret);
                }
            }
        }

        private Node[] BuildTree()
        {
            var roots = _manager.Nodes.Where(n => !n.Address.IsSubNode).Select(n => new Node(n)).ToList();
            foreach (var node in _manager.Nodes.Where(n => n.Address.IsSubNode))
            {
                var root = roots.FirstOrDefault(r => r.TcpNode.Address.Equals(node.Address.SubNode(0)));
                if (root != null)
                {
                    root.Children = root.Children.Concat(new[] { new Node(node) }).ToArray();
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
                await stream.WriteMessage(ms.ToArray());
            }
        }

        private async Task<T> Receive<T>(MessageChannel stream) where T : class
        {
            var buffer = await stream.ReadMessage();
            if (buffer == null)
            {
                return null;
            }
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                return (T)new DataContractSerializer(typeof(T)).ReadObject(ms);
            }
        }

        private void CreateDevice(SinkPath sinkPath, string deviceType, string argument)
        {
            Manager.GetService<DeviceManager>().CreateAndLoadDevice(deviceType, argument, sinkPath);
        }
    }
}
