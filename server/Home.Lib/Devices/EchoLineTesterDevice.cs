using Lucky.Home.Sinks;

namespace Lucky.Home.Devices
{
    [Device("Serial Echo")]
    [Requires(typeof(DuplexLineSink))]
    public class EchoLineTesterDevice : DeviceBase
    {


    }
}
