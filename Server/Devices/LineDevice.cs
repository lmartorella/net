using Lucky.Home.Sinks;

namespace Lucky.Home.Devices
{
    internal class LineDevice : DeviceBase<IDisplaySink>
    {
        public void Write(string str)
        {
            if (IsOnline)
            {
                Sink.Write(str);
            }
        }
    }
}
