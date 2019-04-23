﻿using System.Threading;
using Lucky.Services;
using Lucky.Home.Sinks;

// ReSharper disable UnusedMember.Global

namespace Lucky.Home.Devices
{
    [Device("Temperature Tester")]
    [Requires(typeof(TemperatureSink))]
    public class TemperatureTesterDevice : DeviceBase
    {
        private Timer _timer;

        public TemperatureTesterDevice()
        {
            _timer = new Timer(async o =>
            {
                if (IsFullOnline)
                {
                    TemperatureReading reading = await ((TemperatureSink)Sinks[0]).Read();
                    if (reading.SinkStatus == TemperatureSinkStatus.Ok)
                    {
                        Logger.Log("Reading", "RH%", ToDec(reading.Humidity), "T(C)", ToDec(reading.Temperature));
                    }
                }

            }, null, 0, 2000);
        }

        private static float ToDec(short bigEndian)
        {
            sbyte decPart = (sbyte)(bigEndian >> 8);
            byte intPart = (byte)(bigEndian & 0xff);
            return intPart + decPart / 256.0f;
        }
    }
}