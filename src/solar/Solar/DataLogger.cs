using Lucky.Home.Db;
using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;

namespace Lucky.Home.Solar;

/// <summary>
/// Logs solar power immediate readings and stats.
/// Manages csv files as DB.
/// </summary>
class DataLogger(InverterDevice inverterDevice, NotificationService notificationService, NotificationSender notificationSender, FsTimeSeries<PowerData, DayPowerData> database, ResourceService resourceService, CurrentSensorDevice currentSensorDevice) : BackgroundService
{
    private string _lastFault = null;
    private CurrentSensorData? _lastCurrentSensorData;
    private DateTime? _lastFaultMessageTimeStamp;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        inverterDevice.NewData += (o, e) => HandleNewData(e);
        currentSensorDevice.DataChanged += (o, e) => { _lastCurrentSensorData = currentSensorDevice.LastData; };
    }

    private void HandleNewData(PowerData data)
    {
        // Don't log OFF states
        if (inverterDevice.DeviceState == DeviceState.Offline)
        {
            return;
        }

        data.HomeUsageCurrentA = _lastCurrentSensorData?.Home ?? -1;
        data.GridCurrentA2 = _lastCurrentSensorData?.Export ?? -1;

        database.AddNewSample(data);
        if (data.PowerW > 0)
        {
            // New data, unlock next mail
            notificationSender.OnNewData();
        }

        CheckFault(data.InverterState);
    }

    public PowerData ImmediateData { get; private set; }

    private void CheckFault(InverterState inverterState)
    {
        var fault = inverterState.IsFaultToNotify();
        if (_lastFault != fault)
        {
            DateTime ts = DateTime.Now;
            if (fault != null)
            {
                // Enter fault
                notificationService.EnqueueStatusUpdate(resourceService.GetString<DataLogger>("solar_error_mail_title"), ts.ToString("HH:mm:ss") + ": " + string.Format(resourceService.GetString<DataLogger>("solar_error_mail_error"), fault));
                _lastFaultMessageTimeStamp = ts;
            }
            else
            {
                // Recover
                // Try to recover last message update
                notificationService.EnqueueStatusUpdate(
                    resourceService.GetString<DataLogger>("solar_error_mail_title"), 
                    ts.ToString("HH:mm:ss") + ": " + resourceService.GetString<DataLogger>("solar_error_mail_normal"),
                    _lastFaultMessageTimeStamp.HasValue ?
                        string.Format(resourceService.GetString<DataLogger>("solar_error_mail_error_solved"), (int)(ts - _lastFaultMessageTimeStamp.Value).TotalSeconds) :
                        null
                );
            }
            _lastFault = fault;
        }
    }

    public PowerData GetLastSample()
    {
        return database.GetLastSample();
    }

    public DayPowerData GetAggregatedData()
    {
        return database.GetAggregatedData();
    }
}
