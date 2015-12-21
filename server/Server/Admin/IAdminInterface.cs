using System;
using System.Threading.Tasks;
using Lucky.Home.Devices;

namespace Lucky.Home.Admin
{
    public interface IAdminInterface
    {
        Task<Node[]> GetTopology();
        Task<string[]> GetDeviceTypes();
        Task<bool> RenameNode(string nodeAddress, Guid oldId, Guid newId);
        Task<string> CreateDevice(SinkPath sinkPath, string deviceType, string argument);
    }
}
