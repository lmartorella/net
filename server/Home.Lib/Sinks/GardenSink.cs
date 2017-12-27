using Lucky.Home.Serialization;
using System;
using System.Linq;

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

        public bool Read()
        {
            bool isAvail = false;
            Read(reader =>
            {
                var md = reader.Read<ReadStatusMessageResponse>();
                if (md != null)
                {
                    Console.WriteLine("== GARDEN MD: State {0}, Count {1}, Times: {2}", md.State, md.ZoneTimes.Length, string.Join(", ", md.ZoneTimes.Select(t => t.ToString())));
                    isAvail = md.State == DeviceState.Off;
                }
                else
                {
                    Console.WriteLine("== GARDEN MD: NO DATA");
                }
            });
            return isAvail;
        }

        public void WriteProgram(int[] zoneTimes)
        {
            Write(writer =>
            {
                writer.Write(new WriteStatusMessageRequest { ZoneTimes = zoneTimes.Select(t => (byte)t).ToArray() });
            });
        }
    }
}
