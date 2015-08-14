using Lucky.Home.Core;

namespace Lucky.Home.Sinks
{
    public interface ISystemSink : ISink
    {
        NodeStatus Status { get; }
    }
}