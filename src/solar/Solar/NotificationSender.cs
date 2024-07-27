using Lucky.Home.Db;
using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Home.Solar;

internal class NotificationSender(ILogger<NotificationSender> logger, InverterDevice inverterDevice, FsTimeSeries<PowerData, DayPowerData> database, NotificationService notificationService, ResourceService resourceService) : BackgroundService
{
    private bool _isSummarySent = true;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        inverterDevice.NightStateChanged += (o, e) => HandleStateChanged(inverterDevice.NightState);
    }

    private void HandleStateChanged(NightState state)
    {
        // From connected/connecting to OFF mean end of the day
        if (state == NightState.Night)
        {
            // Send summary
            var summary = database.GetAggregatedData();
            // Skip the first migration from day to night at startup during night
            if (summary != null && !_isSummarySent)
            {
                _ = SendSummaryMail(summary);
                _isSummarySent = true;
            }
        }
    }

    public void OnNewData()
    {
        _isSummarySent = false;
    }

    private async Task SendSummaryMail(DayPowerData day)
    {
        var title = string.Format(resourceService.GetString<DataLogger>("solar_daily_summary_title"), day.PowerKWh);
        var body = resourceService.GetString<DataLogger>("solar_daily_summary")
                .Replace("{PowerKWh}", day.PowerKWh.ToString("0.0"))
                .Replace("{PeakPowerW}", day.PeakPowerW.ToString())
                .Replace("{PeakPowerTimestamp}", day.FromInvariantTime(day.PeakPowerTimestamp).ToString("hh\\:mm\\:ss"))
                .Replace("{PeakVoltageV}", day.PeakVoltageV.ToString())
                .Replace("{PeakVoltageTimestamp}", day.FromInvariantTime(day.PeakVoltageTimestamp).ToString("hh\\:mm\\:ss"))
                .Replace("{SunTime}", (day.Last - day.First).ToString(resourceService.GetString<DataLogger>("solar_daylight_format")));

        logger.LogInformation("DailyMailSending: Power {0}", day.PowerKWh);
        await notificationService.SendMail(title, body, false);
        logger.LogInformation("DailyMailSent: Power {0}", day.PowerKWh);
    }
}
