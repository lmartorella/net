﻿using System.Linq;

namespace Lucky.Home.Devices.Garden
{
    internal class ImmediateProgram
    {
        public ZoneTime[] ZoneTimes;
        public string Name;

        public bool IsEmpty
        {
            get
            {
                return ZoneTimes == null || ZoneTimes.All(z => z.Minutes <= 0);
            }
        }
    }
}

