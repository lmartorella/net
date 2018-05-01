using Lucky.Home.Serialization;
using Lucky.Services;
using System.Linq;

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

        public bool Read(bool log)
        {
            bool isAvail = false;
            Read(reader =>
            {
                var md = reader.Read<ReadStatusMessageResponse>();
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

        public void WriteProgram(int[] zoneTimes)
        {
            if (zoneTimes.All(z => z <= 0))
            {
                return;
            }

            Write(writer =>
            {
                writer.Write(new WriteStatusMessageRequest { ZoneTimes = zoneTimes.Select(t => (byte)t).ToArray() });
            });
            // Log aloud new state
            Read(true);
        }
    }
}
