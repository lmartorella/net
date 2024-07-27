using Lucky.Home;
using Lucky.Home.Db;
using Lucky.Home.Services;
using Lucky.Home.Solar;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Home.Solar
{
    class Program
    {
        private static event Action _dayRotation;
        private static DateTime _nextPeriodStart;
        private static readonly TimeSpan PeriodLength = TimeSpan.FromDays(1);
        private static Timer _timerMinute;

        private const string DeviceHostName = "localhost";

        public static async Task Main(string[] arguments)
        {
            try
            {
                await _Main(arguments);
            }
            catch (Exception exc)
            {
                Manager.GetService<LoggerFactory>().Create("Main").Exception(exc);
            }
        }

        private static async Task _Main(string[] arguments)
        {
            await Bootstrap.Start(arguments, "solar");

            var db = new FsTimeSeries<PowerData, DayPowerData>("SOLAR");
            await db.Init(DateTime.Now);
            _dayRotation += async () => await db.Rotate(DateTime.Now);

            // Rotate solar db at midnight 
            var periodStart = _nextPeriodStart = DateTime.Now.Date;
            while (DateTime.Now >= _nextPeriodStart)
            {
                _nextPeriodStart += PeriodLength;
            }
            _timerMinute = new Timer(s =>
            {
                if (DateTime.Now >= _nextPeriodStart)
                {
                    while (DateTime.Now >= _nextPeriodStart)
                    {
                        _nextPeriodStart += PeriodLength;
                    }
                    _dayRotation?.Invoke();
                }
            }, null, 0, 30 * 1000);


            var ammeter = new AnalogIntegrator();
            var inverter = new InverterDevice();
            var notificationSeder = new NotificationSender(inverter, db, Manager.GetService<INotificationService>());
            var dataLogger = new DataLogger(inverter, ammeter, notificationSeder, db);
            var userInterface = new UserInterface(dataLogger, inverter, ammeter);

            await StartModbusBridges();
        }

        /// <summary>
        /// Start modbus to mqtt bridges
        /// </summary>
        private static async Task StartModbusBridges()
        {
            await Manager.Run();
            Manager.GetService<LoggerFactory>().Create("Main").Log("LoopExited: killed");
        }
    }
}
