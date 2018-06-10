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

        public async Task<bool> Read(bool log)
        {
            bool isAvail = false;
            await Read(async reader =>
            {
                var md = await reader.Read<ReadStatusMessageResponse>();
                if (md != null)
                {
                    if (log)
                    {
                        Logger.Log("GardenMd", "State", md.State, "Times", string.Join(", ", md.ZoneTimes.Select(t => t.ToString())));
                    }
                    isAvail = md.State == DeviceState.Off;
                }
                else
                {
                    Logger.Log("GardenMd NO DATA");
                }
            });
            return isAvail;
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
