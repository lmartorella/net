using System;
using System.Threading;
using Lucky.Home.Sinks.App;

namespace Lucky.Home.Devices.App
{
    [Device(new[] { typeof(TemperatureSink) })]
    class TemperatureDevice : DeviceBase
    {
        private Timer _timer;

        public TemperatureDevice()
        {
            _timer = new Timer(async o =>
            {
                if (IsFullOnline)
                {
                    byte[] reading = await ((TemperatureSink) Sinks[0].Sink).Read();
                    Console.WriteLine(reading);
                }

            }, null, 0, 2000);
        }
    }
}
