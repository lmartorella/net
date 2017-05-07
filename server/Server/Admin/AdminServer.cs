using System;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Home.Devices;
using Lucky.Home.Protocol;
using Lucky.Home.Sinks;
using Lucky.Services;

namespace Lucky.Home.Admin
{
    class AdminServer : IAdminInterface
    {
        private readonly INodeManager _manager;

        public AdminServer()
        {
            _manager = Manager.GetService<NodeManager>();
        }

        public Task<Node[]> GetTopology()
        {
            return Task.FromResult(BuildTree());
        }

        public Task<DeviceTypeDescriptor[]> GetDeviceTypes()
        {
            return Task.FromResult(Manager.GetService<DeviceManager>().DeviceTypes);
        }

        public Task<bool> RenameNode(string nodeAddress, Guid oldId, Guid newId)
        {
            ITcpNode node;
            if (oldId == Guid.Empty)
            {
                node = _manager.FindNode(TcpNodeAddress.Parse(nodeAddress));
            }
            else
            {
                node = _manager.FindNode(oldId);
            }
            if (node != null)
            {
                node.Rename(newId);
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task ResetNode(Guid id, string nodeAddress)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            ITcpNode node;
            if (id != Guid.Empty)
            {
                node = _manager.FindNode(id);
            }
            else
            {
                node = _manager.FindNode(TcpNodeAddress.Parse(nodeAddress));
            }

            if (node != null)
            {
                node.Sink<SystemSink>().Reset();
            }
        }

        public Task<string> CreateDevice(DeviceDescriptor descriptor)
        {
            string err = null;
            try
            {
                Manager.GetService<DeviceManager>().CreateAndLoadDevice(descriptor);
            }
            catch (Exception exc)
            {
                err = exc.Message;
            }
            return Task.FromResult(err);
        }

        public Task<DeviceDescriptor[]> GetDevices()
        {
            return Task.FromResult(Manager.GetService<DeviceManager>().GetDeviceDescriptors());
        }

        public Task DeleteDevice(Guid id)
        {
            Manager.GetService<DeviceManager>().DeleteDevice(id);
            return Task.Run(() => { });
        }

        private Node BuildNode(ITcpNode tcpNode)
        {
            var systemSink = tcpNode.Sink<ISystemSink>();
            return new Node()
            {
                Id = tcpNode.Id,
                Status = systemSink != null ? systemSink.Status : null,
                Address = tcpNode.Address.ToString(),
                Sinks = tcpNode.Sinks.Select(s => s.FourCc).ToArray(),
                SubSinkCount = tcpNode.Sinks.Select(s => s.SubCount).ToArray(),
                IsZombie = tcpNode.IsZombie
            };
        }

        private Node[] BuildTree()
        {
            var roots = _manager.Nodes.Where(n => !n.Address.IsSubNode).Select(BuildNode).ToList();
            foreach (var node in _manager.Nodes.Where(n => n.Address.IsSubNode))
            {
                var root = roots.FirstOrDefault(r => r.Address == node.Address.SubNode(0).ToString());
                if (root != null)
                {
                    root.Children = root.Children.Concat(new[] { BuildNode(node) }).ToArray();
                }
            }
            return roots.ToArray();
        }
    }
}
