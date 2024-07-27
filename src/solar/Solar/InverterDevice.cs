﻿using Lucky.Home.Services;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Solar
{
    /// <summary>
    /// Subscribe the inverter device's MQTT topics and exposes C# events for the loggers.
    /// </summary>
    class InverterDevice
    {
        public InverterDevice()
        {
            _ = Subscribe();
        }

        private async Task Subscribe()
        {
            // Send inverter data to the data logger and notification server
            await Manager.GetService<MqttService>().SubscribeJsonTopic(Constants.SolarDataTopicId, async (PowerData data) =>
            {
                NewData?.Invoke(this, data);
            });
            await Manager.GetService<MqttService>().SubscribeRawTopic(Constants.SolarStateTopicId, async data =>
            {
                if (Enum.TryParse(Encoding.UTF8.GetString(data), out NightState))
                {
                    NightStateChanged?.Invoke(this, NightState);
                }
            });
        }

        /// <summary>
        /// Event raised when new data comes from the inverter
        /// </summary>
        public event EventHandler<PowerData> NewData;

        /// <summary>
        /// Event raised when <see cref="NightState"/> changes.
        /// </summary>
        public event EventHandler<NightState> NightStateChanged;

        /// <summary>
        /// The low-level inverter state
        /// </summary>
        public NightState NightState;
    }
}
