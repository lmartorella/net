using System.Threading.Tasks;
using Lucky.Home.Core;

namespace Lucky.Home.Sinks
{
    public interface ISystemSink : ISink
    {
        Task<NodeStatus> GetBootStatus();
    }
}