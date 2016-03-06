using System;
using System.Linq;
using System.Threading;
using Lucky.Home.Sinks.App;
using Lucky.Services;

namespace Lucky.Home.Devices.App
{
    [Device(new[] { typeof(TemperatureSink) })]
    class TemperatureDevice : DeviceBase
    {
        private Timer _timer;
        private ILogger Logger { get; set; }

        public TemperatureDevice()
        {
            Logger = Manager.GetService<LoggerFactory>().Create("TempDevice");

            _timer = new Timer(async o =>
            {
                if (IsFullOnline)
                {
                    byte[] reading = await ((TemperatureSink) Sinks[0].Sink).Read();
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

        private float ToDec(short bigEndian)
        {
            sbyte decPart = (sbyte)(bigEndian >> 8);
            byte intPart = (byte)(bigEndian & 0xff);
            return intPart + decPart / 256.0f;
        }
    }
}
