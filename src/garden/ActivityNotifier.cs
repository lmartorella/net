using System.Text;
using Lucky.Garden.Device;
using Lucky.Home.Notification;
using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Garden;

/// <summary>
/// Send notifications when the garden does a cycle
/// </summary>
class ActivityNotifier(ILogger<ActivityNotifier> logger, ShellyEvents shellyEvents, MqttService mqttService, ConfigService configService) : BackgroundService
{
    private bool masterOutput = false;
    private Dictionary<int, List<Tuple<bool, DateTime>>> events = new Dictionary<int, List<Tuple<bool, DateTime>>>();
    private MqttService.RpcOriginator rpcCaller = null!;

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        shellyEvents.OutputChanged += (o, e) =>
        {
            e.Waiters.Add(ProcessMessage(e));
        };

        rpcCaller = await mqttService.RegisterRpcOriginator("notification/send_mail");
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
            logger.LogInformation("SendNotification: {0} at {1}", builder.ToString(), DateTime.Now);
            await rpcCaller.JsonRemoteCall<SendMailRequestMqttPayload, RpcVoid>(
                new SendMailRequestMqttPayload { Title = "Activity", Body = builder.ToString(), IsAdminReport = false },
                true  // Don't wait for response
            );
        }
        else
        {
            logger.LogInformation("SendNotification, zero events");
        }
    }
}
