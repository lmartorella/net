using System.Linq;
using Lucky.Home.Sinks;
using Lucky.Home.Sinks.App;

namespace Lucky.Home.Devices.App
{
    [Device(new [] {typeof(IDisplaySink)})]
    internal class LineDevice : DeviceBase
    {
        public void Write(string str)
        {
            if (IsFullOnline)
            {
                foreach (var sink in Sinks.OfType<IDisplaySink>())
                {
                    sink.Write(str);
                }
            }
        }
    }
}
