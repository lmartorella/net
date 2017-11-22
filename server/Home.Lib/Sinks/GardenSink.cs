using Lucky.Home.Serialization;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        public void Read()
        {
            Read(reader =>
            {
                var md = reader.Read<ReadStatusMessageResponse>();
                if (md != null)
                {
                    Console.WriteLine("== GARDEN MD: State {0}, Count {1}, Times: {2}", md.State, md.ZoneTimes.Length, string.Join(", ", md.ZoneTimes.Select(t => t.ToString())));
                }
                else
                {
                    Console.WriteLine("== GARDEN MD: NO DATA");
                }
            });
        }
    }
}
