using System;
using Lucky.Services;
using Lucky.Home.Devices;
using System.Linq;
using Lucky.Home.Db;
using System.Threading;
using Lucky.Home.Power;

namespace Lucky.Home.Application
{
    /// <summary>
    /// The home application
    /// </summary>
    class HomeApp : AppService
    {
        private Timer _timerMinute;
        private event Action _dayRotation;
        private DateTime _nextPeriodStart;
        private static readonly TimeSpan PeriodLenght = TimeSpan.FromDays(1);

        /// <summary>
        /// Fetch all devices. To be called when the list of the devices changes
        /// </summary>
        public override void Run()
        {
            var deviceMan = Manager.GetService<IDeviceManager>();
            IDevice[] devices;
            lock (deviceMan.DevicesLock)
            {
                devices = deviceMan.Devices.ToArray();
            }

            // Process all device created
            foreach (var device in devices.OfType<ISolarPanelDevice>())
            {
                IFsTimeSeries db;
                if (device is SamilInverterLoggerDevice)
                {
                    var dbd = new FsTimeSeries<SamilPowerData>(device.Name, "HH:mm:ss", false);
                    ((SamilInverterLoggerDevice)device).Database = dbd;
                    db = dbd;
                }
                else
                {
                    var dbd = new FsTimeSeries<PowerData>(device.Name, "HH:mm:ss", false);
                    device.Database = dbd;
                    db = dbd;
                }
                db.Rotate(ToPowerCvsName(DateTime.Now), DateTime.Now);
                _dayRotation += () => db.Rotate(ToPowerCvsName(DateTime.Now), DateTime.Now);
            }

            // Rotate solar db at midnight 
            var periodStart = _nextPeriodStart = DateTime.Now.Date;
            while (DateTime.Now >= _nextPeriodStart)
            {
                _nextPeriodStart += PeriodLenght;
            }
            _timerMinute = new Timer(s => 
            {
                if (DateTime.Now >= _nextPeriodStart)
                {
                    while (DateTime.Now >= _nextPeriodStart)
                    {
                        _nextPeriodStart += PeriodLenght;
                    }
                    _dayRotation?.Invoke();
                }
            }, null, 0, 30 * 1000);

            base.Run();
        }

        private static string ToPowerCvsName(DateTime now)
        {
            return now.ToString("yyyy-MM-dd") + ".csv";
        }
    }
}
