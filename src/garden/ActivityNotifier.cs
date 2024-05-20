using System.Text;
using Lucky.Garden.Device;
using Lucky.Home.Notification;
using Microsoft.Extensions.Hosting;

namespace Lucky.Garden;

/// <summary>
/// Send notifications when the garden does a cycle
/// </summary>
class ActivityNotifier(ShellyEvents shellyEvents, INotificationService notificationService, ConfigService configService) : BackgroundService
{
    private bool masterOutput = false;
    private Dictionary<int, List<Tuple<bool, DateTime>>> events = new Dictionary<int, List<Tuple<bool, DateTime>>>();

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        shellyEvents.OutputChanged += (o, e) =>
        {
            e.Waiters.Add(ProcessMessage(e));
        };
        return Task.CompletedTask;
    }

    private async Task ProcessMessage(ShellyEvents.OutputEvent e)
    {
        if (e.Id == 0)
        {
            masterOutput = e.Output;
            if (!masterOutput)
            {
                await SendNotification();
            }
            else
            {
                // Reset cycle
                events.Clear();
            }
        }
        else
        {
            RecordEvent(e.Id, e.Output);
        }
    }

    private void RecordEvent(int id, bool state)
    {
        List<Tuple<bool, DateTime>> list;
        if (!events.TryGetValue(id, out list!)) 
        {
            list = new List<Tuple<bool, DateTime>>();
            events[id] = list;
        }

        list.Add(Tuple.Create(state, DateTime.Now));
    }

    private async Task SendNotification()
    {
        StringBuilder builder = new StringBuilder();
        var config = await configService.GetConfig();

        // Generates the list of the areas run
        foreach (var entry in events)
        {
            int id = entry.Key - 1;
            string zoneName = $"{entry.Key}";
            if (id >= 0 && id < config.Zones.Length)
            {
                zoneName = config.Zones[id];
            }

            bool lastState = false;
            DateTime lastTimeStamp = DateTime.MinValue;
            foreach (var ev in entry.Value)
            {
                if (ev.Item1 == lastState)
                {
                    continue;
                }
                lastState = ev.Item1;
                if (lastState)
                {
                    // Switched on at
                    lastTimeStamp = ev.Item2;
                }
                else
                {
                    // Switched off and lasted
                    var duration = ev.Item2 - lastTimeStamp;
                    builder.Append($"Zone {zoneName} ran for {duration}");
                    builder.Append(Environment.NewLine);
                }
            }
        }

        if (builder.Length > 0)
        {
            await notificationService.SendMail("Activity", builder.ToString(), false);
        }
    }
}
