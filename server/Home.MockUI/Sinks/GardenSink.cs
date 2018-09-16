using Lucky.HomeMock.Core;
using System.IO;
using System.Linq;
using System.Threading;

namespace Lucky.HomeMock.Sinks
{
    class GardenSink : SinkMockBase
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

        private class Cycles
        {
            public int Zones;
            public int Minutes;

            public override string ToString()
            {
                return "Z:0x" + Zones.ToString("X2") + ":" + Minutes + "min";
            }
        }

        private Cycles[] _cycles;
        private DeviceState _state = DeviceState.Off;
        private Timer _timer;
        private readonly object _lockObject = new object();

        public GardenSink() : base("GARD")
        {
            _cycles = Enumerable.Range(0, 5).Select(i => new Cycles { Zones = 0, Minutes = 0 }).ToArray();
        }

        public override void Read(BinaryReader reader)
        {
            // Read new program
            short count = reader.ReadInt16();
            var times = reader.ReadBytes(count * 2);

            lock (_lockObject)
            {
                if (_state == DeviceState.Off)
                {
                    for (int i = 0; i < count; i++)
                    {
                        _cycles[i] = new Cycles { Minutes = times[i * 2], Zones = times[i * 2 + 1] };
                    }
                    Log(string.Format("Garden timer: {0}", string.Join(", ", _cycles.Select(t => t.ToString()))));
                    StartProgram();
                }
                else
                {
                    Log("Program ignored: " + _state);
                }
            }
        }

        private void StartProgram()
        {
            lock (_lockObject)
            {
                _state = DeviceState.InUse;
                _timer = new Timer(o =>
                {
                    lock (_lockObject)
                    {
                        if (_cycles.All(t => t.Minutes == 0))
                        {
                            _state = DeviceState.Off;
                            _timer.Dispose();
                            _timer = null;
                        }
                        else
                        {
                            for (int i = 0; i < _cycles.Length; i++)
                            {
                                if (_cycles[i].Minutes > 0)
                                {
                                    _cycles[i].Minutes--;
                                    break;
                                }
                            }
                        }
                    }
                }, null, 3000, 3000);
            }
        }

        public override void Write(BinaryWriter writer)
        {
            lock (_lockObject)
            {
                writer.Write((byte)_state);
                writer.Write((short)_cycles.Length);
                writer.Write(_cycles.Select(z => new byte[] { (byte)z.Minutes, (byte)z.Zones }).SelectMany(b => b).ToArray());
            }
        }
    }
}
