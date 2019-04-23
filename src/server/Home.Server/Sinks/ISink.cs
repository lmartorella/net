using Lucky.Home.Devices;
using Lucky.Home.Protocol;

namespace Lucky.Home.Sinks
{
    public interface ISink
    {
        SinkPath Path { get; }
        string FourCc { get; }
        int SubCount { get; }
        bool IsOnline { get; }
    }

    internal interface ISinkInternal : ISink
    {
        ITcpNode Node { get; }
    }
}