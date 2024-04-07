using Lucky.Home.Protocol;
using System.Threading.Tasks;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// System sink 
    /// </summary>
    internal interface ISystemSink : ISink
    {
        NodeStatus Status { get; }
        Task Reset();
    }
}