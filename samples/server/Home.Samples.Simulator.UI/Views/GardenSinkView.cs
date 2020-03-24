using Lucky.Home.Services;
using Lucky.Home.Simulator;
using System.IO;
using System.Linq;
using System.Threading;

namespace Lucky.Home.Views
{
    /// <summary>
    /// Mock sink for the garden programmer
    /// </summary>
    [MockSink("GARD", "Garden")]
    public partial class GardenSinkView : ISinkMock
    {
        private ILogger Logger;

        private enum DeviceState : byte
        {
            Off = 0,
            // Immediate program mode
            ProgramImmediate,
            // Display flow level
            FlowCheck,
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

        public GardenSinkView()
        {
            _cycles = Enumerable.Range(0, 5).Select(i => new Cycles { Zones = 0, Minutes = 0 }).ToArray();
        }

        public void Init(ISimulatedNode node)
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("GardenSink", node.Id.ToString());
            node.IdChanged += (o, e) => Logger.SubKey = node.Id.ToString();
        }

        public void Read(BinaryReader reader)
        {
            // Read new program
            short count = reader.ReadInt16();
            if (count == -1)
            {
                // Flow data
                var flow = reader.ReadInt16();
                Logger.Log(string.Format("Flow data received: {0} lt/min", flow));
            }
            else
            {
                var times = reader.ReadBytes(count * 2);
                lock (_lockObject)
                {
                    if (_state == DeviceState.Off)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            _cycles[i] = new Cycles { Minutes = times[i * 2], Zones = times[i * 2 + 1] };
                        }
                        Logger.Log(string.Format("Garden timer: {0}", string.Join(", ", _cycles.Select(t => t.ToString()))));
                        StartProgram();
                    }
                    else
                    {
                        Logger.Log("Program ignored: " + _state);
                    }
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

        public void Write(BinaryWriter writer)
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
