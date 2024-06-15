using System.Runtime.Serialization;
using Lucky.Home.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Lucky.Garden.Device;

/// <summary>
/// Subscribe device events to .NET
/// </summary>
class ShellyEvents(ILogger<ShellyEvents> logger, Configuration configuration, MqttService mqttService): BackgroundService
{
    private const int OutputCount = 3;

    [DataContract]
    public class OutputTopicEvent
    {
        /// <summary>
        /// Output ID (0-2)
        /// </summary>
        [DataMember(Name = "id")]
        public int Id;

        [DataMember(Name = "output")]
        public bool Output;

        /// <summary>
        /// In seconds
        /// </summary>
        [DataMember(Name = "timer_duration", IsRequired = false)]
        public double? TimerDuration;
    }

    public class OutputState
    {
        public bool? Output;

        public double? TimerDuration;
    }

    public class OutputEventArgs(int id, OutputState state) 
    {
        public int Id { get; } = id;
        public OutputState State { get; } = state;
        public readonly List<Task> Waiters = new List<Task>();
    }

    /// <summary>
    /// Shelly sends multiple pub at poll, so check if changed before raise the event
    /// </summary>
    private OutputState[] lastStates = Enumerable.Range(0, 3).Select(i => new OutputState()).ToArray();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation($"Subscribing switch events");
        for (int i = 0; i < OutputCount; i++) {
            var topic = $"{configuration.DeviceId}/status/switch:{i}";
            await mqttService.SubscribeJsonTopic<OutputTopicEvent>(topic, async data => 
            {
                if (data == null)
                {
                    return;
                }

                int id = data.Id;
                if (lastStates[id].Output == data.Output && lastStates[id].TimerDuration == data.TimerDuration)
                {
                    return;
                }

                var args = new OutputEventArgs(id, new OutputState { Output = data.Output, TimerDuration = data.TimerDuration });
                if (data.TimerDuration.HasValue)
                {
                    logger.LogInformation($"Output {data.Id} changed to {data.Output} for {data.TimerDuration} seconds");
                }
                else
                {
                    logger.LogInformation($"Output {data.Id} changed to {data.Output}");
                }
                OutputChanged?.Invoke(this, args);
                await Task.WhenAll(args.Waiters);
            });
        }
    }

    public event EventHandler<OutputEventArgs>? OutputChanged;
}
