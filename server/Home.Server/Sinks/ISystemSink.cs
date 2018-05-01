using System.Threading.Tasks;

namespace Lucky.Home.Sinks
{
    internal interface ISystemSink : ISink
    {
        NodeStatus Status { get; }
        void Reset();
    }
}