using Lucky.Serialization;
using Lucky.Services;
using System.Linq;
using System.Threading.Tasks;

#pragma warning disable 649

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Passive/poll based switch array
    /// </summary>
    [SinkId("GARD")]
    class GardenSink : SinkBase
    {
        private enum DeviceState : byte
        {
            Off = 0,
            // Immediate program mode
            ProgramImmediate,
            // Display water level (future usage))
            LevelCheck,
            // Program the timer mode
            ProgramTimer,
            // Looping a program (manual or automatic)
            InUse,
            // Timer used after new programming, while the display shows OK, to go back to imm state (2 seconds)
            WaitForImmediate
        }

        private class ReadStatusMessageResponse
        {
            public DeviceState State;

            [SerializeAsDynArray]
            public byte[] ZoneTimes;
        }

        private class WriteStatusMessageRequest
        {
            [SerializeAsDynArray]
            public byte[] ZoneTimes;
        }

        public class TimerState
        {
            public bool IsAvailable;
            public int[] ZoneRemTimes;
        }

        public async Task<TimerState> Read(bool log, int timeout = 0)
        {
            TimerState state = null;
            await Read(async reader =>
            {
                var md = await reader.Read<ReadStatusMessageResponse>();
                if (md != null)
                {
                    if (log)
                    {
                        Logger.Log("GardenMd", "State", md.State, "Times", string.Join(", ", md.ZoneTimes.Select(t => t.ToString())));
                    }
                    state = new TimerState { IsAvailable = md.State == DeviceState.Off, ZoneRemTimes = md.ZoneTimes.Select(t => (int)t).ToArray() };
                }
                else
                {
                    Logger.Log("GardenMd NO DATA");
                }
            }, timeout);
            return state;
        }

        public async Task WriteProgram(int[] zoneTimes)
        {
            if (zoneTimes.All(z => z <= 0))
            {
                return;
            }

            await Write(async writer =>
            {
                await writer.Write(new WriteStatusMessageRequest { ZoneTimes = zoneTimes.Select(t => (byte)t).ToArray() });
            });
            // Log aloud new state
            await Read(true);
        }
    }
}
