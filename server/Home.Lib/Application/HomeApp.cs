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
    class HomeApp : ServiceBase
    {
        private Timer _timerMinute;
        private event Action _dayRotation;
        private DateTime _nextPeriodStart;
        private static readonly TimeSpan PeriodLenght = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Fetch all devices. To be called when the list of the devices changes
        /// </summary>
        internal void Start()
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
                var db = new FsTimeSeries<PowerData>(string.Format("db/{0}", device.Name), "HH:mm:ss");
                db.Rotate(ToPowerCvsName(DateTime.Now), DateTime.Now);
                device.Database = db;
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
        }

        private static string ToPowerCvsName(DateTime now)
        {
            return now.ToString("yyyy-MM-dd-HH-mm.csv");
        }
    }
}
