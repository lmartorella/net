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
        private int _lastDay;
        private Timer _timerMinute;
        private event EventHandler _dayRotation;

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
                var db = new FsTimeSeries<PowerData>(string.Format("db/{0}", device.Name));
                db.Rotate(ToPowerCvsName(DateTime.Now));
                device.Database = db;
                _dayRotation += (o,e) => db.Rotate(ToPowerCvsName(DateTime.Now));
            }

            // Rotate solar db at midnight 
            _lastDay = DateTime.Now.Day;
            _timerMinute = new Timer(s => {
                if (DateTime.Now.Day != _lastDay)
                {
                    _lastDay = DateTime.Now.Day;
                    _dayRotation?.Invoke(this, EventArgs.Empty);
                }
            }, null, 0, 60 * 1000);
        }

        private static string ToPowerCvsName(DateTime now)
        {
            return now.ToString("yyyy-MM-dd.csv");
        }
    }
}
