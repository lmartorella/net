using System.Threading.Tasks;

namespace Lucky.Home.Admin
{
    /// <summary>
    /// Common interface used by Admin UI and implemented in server code
    /// </summary>
    internal interface IAdminInterface
    {
        Task<Node[]> GetTopology();
        Task<bool> RenameNode(string nodeAddress, NodeId oldId, NodeId newId);
        Task ResetNode(NodeId id, string nodeAddress);
    }
}
