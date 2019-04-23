﻿using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Lucky.Home.Sinks
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
    }

    [SinkId("FLOW")]
    class FlowSink : SinkBase
    {
        /// <summary>
        /// A flow sample
        /// </summary>
        private class GetDataMessage
        {
            /// <summary>
            /// Total counter
            /// </summary>
            public UInt32 Count;

            /// <summary>
            /// Count in the time unit (count/seconds)
            /// </summary>
            public UInt16 Frequency;
        }

        /// <summary>
        /// fq is frequency / L/min (5.5 on sample counter)
        /// </summary>
        public async Task<FlowData> ReadData(double fq)
        {
            // F(tick/sec) = fq * flow(L / Min)
            // flow(L/min) = F(tick/sec) / fq

            // F(tick/min) = fq * 60 * flow(L/min)
            // ticks = fq * 60 * L
            // L = ticks / fq / 60  
            // Mc = ticks / fq / 60.000

            FlowData data = null;
            await Read(async reader =>
            {
                var rawData = await reader.Read<GetDataMessage>();
                if (rawData != null)
                {
                    data = new FlowData { TotalMc = rawData.Count / fq / 60000.0, FlowLMin = rawData.Frequency / fq };
                }
            });
            return data;
        }
    }
}
