using System.Linq;
using Lucky.Home.Sinks;

namespace Lucky.Home.Devices
{
    [Device("Display")]
    [RequiresArray(typeof(DisplaySink))]
    public class DisplayDevice : DeviceBase
    {
        public void Write(string str)
        {
            if (IsFullOnline)
            {
                foreach (var sink in Sinks.OfType<DisplaySink>())
                {
                    sink.Write(str);
                }
            }
        }
    }
}
