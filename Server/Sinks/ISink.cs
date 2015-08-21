using Lucky.Home.Devices;
using Lucky.Home.Protocol;

namespace Lucky.Home.Sinks
{
    internal interface ISink
    {
        SinkPath Path { get; }
        ITcpNode Node { get; }
        string FourCc { get; }
    }
}