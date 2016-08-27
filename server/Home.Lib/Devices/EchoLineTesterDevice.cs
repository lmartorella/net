using Lucky.Home.Sinks;

namespace Lucky.Home.Devices
{
    [Device("Serial Echo")]
    [Requires(typeof(HalfDuplexLineSink))]
    public class EchoLineTesterDevice : DeviceBase
    {


    }
}
