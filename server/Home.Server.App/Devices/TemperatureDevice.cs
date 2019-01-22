using System;
using System.Linq;
using System.Threading;
using Lucky.Services;
using Lucky.Home.Sinks;

// ReSharper disable UnusedMember.Global

namespace Lucky.Home.Devices
{
    [Device("Temperature")]
    [Requires(typeof(TemperatureSink))]
    public class TemperatureDevice : DeviceBase
    {
        private Timer _timer;

        public TemperatureDevice()
        {
            _timer = new Timer(async o =>
            {
                if (IsFullOnline)
                {
                    byte[] reading = await ((TemperatureSink)Sinks[0]).Read();
                    if (reading == null || reading.Length != 6)
                    {
                        Logger.Error("ProtocolError", "Len", reading != null ? reading.Length : -1);
                        return;
                    }
                    if (reading[0] != 1)
                    {
                        Logger.Error("BeanErrorReadingSensor");
                        return;
                    }
                    // Calc checksum
                    if (reading.Skip(1).Take(4).Sum(b => b) != reading[5])
                    {
                        Logger.Error("ChecksumError");
                        return;
                    }
                    short u1 = BitConverter.ToInt16(reading, 1);
                    short u2 = BitConverter.ToInt16(reading, 3);
                    Logger.Log("Reading", "RH%", ToDec(u1), "T(C)", ToDec(u2));
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
