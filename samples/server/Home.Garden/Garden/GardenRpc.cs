using Lucky.Home.Services;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lucky.Home.Devices.Garden
{
    class GardenRpc : BaseRpc
    {
        [DataContract]
        public class ImmediateZoneTime
        {
            [DataMember(Name = "minutes")]
            public int Minutes;

            [DataMember(Name = "zoneMask")]
            public int ZoneMask;
        }

        [DataContract]
        public class TimerState
        {
            [DataMember(Name = "isAvailable")]
            public bool IsAvailable;

            [DataMember(Name = "zoneRemTimes")]
            public ImmediateZoneTime[] ZoneRemTimes;
        }

        [DataContract]
        public class Program
        {
            [DataMember(Name = "times")]
            public ImmediateZoneTime[] Times;
        }

        public GardenRpc()
        {
            _ = mqttService.RegisterRemoteCalls(new[] { "garden_timer_0/reset", "garden_timer_0/state", "garden_timer_0/program", "garden_timer_0/setFlow" });
        }

        public async Task<bool> ResetNode()
        {
            try
            {
                await mqttService.RawRemoteCall("garden_timer_0/reset");
                IsOnline = true;
                return true;
            }
            catch (TaskCanceledException)
            {
                IsOnline = false;
                return false;
            }
        }

        public async Task<TimerState> ReadState()
        {
            try
            {
                var ret = await mqttService.JsonRemoteCall<RpcVoid, TimerState>("garden_timer_0/state");
                IsOnline = true;
                return ret;
            }
            catch (TaskCanceledException)
            {
                IsOnline = false;
                return null;
            }
        }

        public async Task WriteProgram(ImmediateZoneTime[] zoneTimes)
        {
            try
            {
                await mqttService.JsonRemoteCall<Program, RpcVoid>("garden_timer_0/program", new Program { Times = zoneTimes });
                IsOnline = true;
            }
            catch (TaskCanceledException)
            {
                IsOnline = false;
            }
        }

        public async Task UpdateFlowData(int flow)
        {
            try
            {
                await mqttService.RawRemoteCall("garden_timer_0/setFlow", Encoding.UTF8.GetBytes(flow.ToString()));
                IsOnline = true;
            }
            catch (TaskCanceledException)
            {
                IsOnline = false;
            }
        }
    }
}
