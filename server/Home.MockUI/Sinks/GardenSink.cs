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

        private byte[] _times = new byte[] { 0, 0, 0, 0, 0 };
        private DeviceState _state = DeviceState.Off;
        private Timer _timer;
        private readonly object _lockObject = new object();

        public GardenSink() : base("GARD")
        {
        }

        public override void Read(BinaryReader reader)
        {
            // Read new program
            short count = reader.ReadInt16();
            var times = reader.ReadBytes(count);

            lock (_lockObject)
            {
                if (_state == DeviceState.Off)
                {
                    _times = times;
                    Log(string.Format("Garden timer: {0}", string.Join(", ", _times.Select(t => t.ToString()))));
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
                        if (_times.All(t => t == 0))
                        {
                            _state = DeviceState.Off;
                            _timer.Dispose();
                            _timer = null;
                        }
                        else
                        {
                            for (int i = 0; i < _times.Length; i++)
                            {
                                if (_times[i] > 0)
                                {
                                    _times[i]--;
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
                writer.Write((short)_times.Length);
                writer.Write(_times);
            }
        }
    }
}
