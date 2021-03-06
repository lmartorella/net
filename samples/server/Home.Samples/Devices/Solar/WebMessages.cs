﻿using Lucky.Home.Services;
using System.Runtime.Serialization;

namespace Lucky.Home.Devices.Solar
{
    [DataContract]
    public class SolarWebResponse : WebResponse
    {
        /// <summary>
        /// Is the sink online?
        /// </summary>
        [DataMember(Name = "status")]
        public OnlineStatus Status{ get; set; }

        [DataMember(Name = "currentW")]
        public double CurrentW { get; set; }

        [DataMember(Name = "currentTs")]
        public string CurrentTs { get; set; }

        [DataMember(Name = "totalDayWh")]
        public double TotalDayWh { get; set; }

        [DataMember(Name = "totalKwh")]
        public double TotalKwh { get; set; }

        [DataMember(Name = "mode")]
        public int Mode { get; set; }

        [DataMember(Name = "fault")]
        public int Fault { get; set; }

        [DataMember(Name = "peakW")]
        public double PeakW { get; set; }

        [DataMember(Name = "peakTsTime")]
        public string PeakTsTime { get; set; }

        // Home usage power, to report Net Energy Metering
        [DataMember(Name = "gridV")]
        public double GridV { get; set; }
        [DataMember(Name = "usageA")]
        public double UsageA { get; set; }
    }
}
