using Lucky.Home.Services;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Lucky.Home.Devices.Garden
{
    /// <summary>
    /// A flow sample
    /// </summary>
    [DataContract]
    public class FlowData
    {
        /// <summary>
        /// Total counter in m3
        /// </summary>
        [DataMember(Name = "totalMc")]
        public double TotalMc;

        /// <summary>
        /// Current flow in liters/minute
        /// </summary>
        [DataMember(Name = "flowLMin")]
        public double FlowLMin;

        [DataMember(Name = "offline")]
        public bool Offline;
    }

    class FlowRpc : BaseRpc
    {
        public FlowRpc()
        {
            _ = mqttService.RegisterRemoteCalls(new[] { "flow_meter_0/value" });
        }

        /// <summary>
        /// fq is frequency / L/min (5.5 on sample counter)
        /// </summary>
        public async Task<FlowData> ReadData()
        {
            try
            {
                FlowData response = await mqttService.JsonRemoteCall<RpcVoid, FlowData>("flow_meter_0/value");
                IsOnline = !response.Offline;
                return response;
            }
            catch (TaskCanceledException)
            {
                // No data
                IsOnline = false;
                return null;
            }
        }
    }
}
