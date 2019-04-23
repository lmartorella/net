﻿using System;
using Lucky.Services;
using Lucky.Home.Devices;
using System.Linq;
using Lucky.Home.Db;
using System.Threading;
using Lucky.Home.Power;
using System.Threading.Tasks;
using Lucky.Home.Notification;

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
        public async Task Start()
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
                var db = new FsTimeSeries<PowerData, DayPowerData>(device.Name);
                await db.Init(DateTime.Now);
                device.StartLoop(db);
                _dayRotation += async () => await db.Rotate(DateTime.Now);
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

            var mail = Manager.GetService<IConfigurationService>().GetConfig("sendMail");
            if (!string.IsNullOrEmpty(mail))
            {
                await Manager.GetService<INotificationService>().SendMail("Messaggio", mail);
            }
        }
    }
}
