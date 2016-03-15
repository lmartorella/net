using System;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Home.Devices;
using Lucky.Home.Protocol;
using Lucky.Services;

namespace Lucky.Home.Admin
{
    class AdminInterface : IAdminInterface
    {
        private readonly INodeManager _manager;

        public AdminInterface()
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
    }
}
