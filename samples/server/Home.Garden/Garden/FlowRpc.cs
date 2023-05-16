using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
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
    }

    class FlowRpc : BaseRpc
    {
        /// <summary>
        /// fq is frequency / L/min (5.5 on sample counter)
        /// </summary>
        public async Task<FlowData> ReadData(double fq, int timeout = 3000)
        {
            throw new NotImplementedException();
        }
    }
}
