using System;
using System.Threading.Tasks;
using Lucky.Home.Devices;

namespace Lucky.Home.Admin
{
    public interface IAdminInterface
    {
        Task<Node[]> GetTopology();
        Task<DeviceTypeDescriptor[]> GetDeviceTypes();
        Task<bool> RenameNode(string nodeAddress, NodeId oldId, NodeId newId);
        Task<string> CreateDevice(DeviceDescriptor descriptor);
        Task<DeviceDescriptor[]> GetDevices();
        Task DeleteDevice(Guid id);
        Task ResetNode(NodeId id, string nodeAddress);
    }
}
