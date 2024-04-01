using Lucky.Home.Services;
using System;
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

        private Task<MqttService.RpcOriginator> rpcReset;
        private Task<MqttService.RpcOriginator> rpcState;
        private Task<MqttService.RpcOriginator> rpcProgram;
        private Task<MqttService.RpcOriginator> rpcSetFlow;

        public static TimeSpan FlowTimeout = TimeSpan.FromSeconds(1.5);
        public static TimeSpan StateTimeout = TimeSpan.FromSeconds(3);
        public static TimeSpan Timeout = TimeSpan.FromSeconds(2);

        public GardenRpc()
        {
            rpcReset = mqttService.RegisterRpcOriginator("garden_timer_0/reset", Timeout);
            rpcState = mqttService.RegisterRpcOriginator("garden_timer_0/state", StateTimeout);
            rpcProgram = mqttService.RegisterRpcOriginator("garden_timer_0/program", Timeout);
            rpcSetFlow = mqttService.RegisterRpcOriginator("garden_timer_0/setFlow", Timeout);
        }

        public async Task<bool> ResetNode()
        {
            try
            {
                await (await rpcReset).RawRemoteCall();
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
                var ret = await (await rpcState).JsonRemoteCall<RpcVoid, TimerState>();
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
                await (await rpcProgram).JsonRemoteCall<Program, RpcVoid>(new Program { Times = zoneTimes });
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
                await (await rpcSetFlow).RawRemoteCall(Encoding.UTF8.GetBytes(flow.ToString()));
                IsOnline = true;
            }
            catch (TaskCanceledException)
            {
                IsOnline = false;
            }
        }
    }
}
