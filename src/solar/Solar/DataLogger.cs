using Lucky.Home.Db;
using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;

namespace Lucky.Home.Solar;

/// <summary>
/// Logs solar power immediate readings and stats.
/// Manages csv files as DB.
/// </summary>
class DataLogger(InverterDevice inverterDevice, NotificationService notificationService, NotificationSender notificationSender, FsTimeSeries<PowerData, DayPowerData> database, ResourceService resourceService) : BackgroundService
{
    private string _lastFault = null;
    private DateTime? _lastFaultMessageTimeStamp;
    private NightState nightState;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        inverterDevice.NewData += (o, e) => HandleNewData(e);
        inverterDevice.NightStateChanged += (o, e) => HandleNightStateChanged(e);
        nightState = inverterDevice.NightState;
    }

    private void HandleNightStateChanged(NightState e)
    {
        nightState = e;
    }

    private void HandleNewData(PowerData data)
    {
        // Don't log OFF states
        if (nightState == NightState.Night)
        {
            return;
        }
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
