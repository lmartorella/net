using System;
using System.Threading.Tasks;

namespace Lucky.Home.Devices.Garden
{
    class GardenRpc : BaseRpc
    {
        public class ImmediateZoneTime
        {
            public byte Time;
            public byte ZoneMask;
        }

        public class TimerState
        {
            public bool IsAvailable;
            public ImmediateZoneTime[] ZoneRemTimes;
        }

        public async Task ResetNode()
        {
            throw new NotImplementedException();
        }

        public async Task<TimerState> Read(bool log, int timeout = 3000)
        {
            throw new NotImplementedException();
        }

        public async Task WriteProgram(ImmediateZoneTime[] zoneTimes)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateFlowData(int flow)
        {
            throw new NotImplementedException();
        }
    }
}
