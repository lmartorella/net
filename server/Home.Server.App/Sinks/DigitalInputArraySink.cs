using System;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Serialization;

// ReSharper disable UnusedMember.Global
#pragma warning disable 649

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Passive/poll based switch array
    /// </summary>
    [SinkId("DIAR")]
    internal class DigitalInputArraySink : SinkBase
    {
        public TimeSpan PollPeriod = TimeSpan.FromSeconds(5);
        private bool[] _status = new bool[0];

        protected async override Task OnInitialize()
        {
            await base.OnInitialize();
            RunLoop();
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ReadStatusResponse : ISerializable
        {
            /// <summary>
            /// Number of switches
            /// </summary>
            public int SwitchesCount; 

            /// <summary>
            /// Switch data in bytes 
            /// </summary>
            public byte[] Data;

            public async Task Deserialize(Func<int, Task<byte[]>> feeder)
            {
                byte[] size = await feeder(1);
                // Size is in bits
                SwitchesCount = size[0];
                int len = (SwitchesCount - 1) / 8 + 1;
                Data = await feeder(len);
            }

            public byte[] Serialize()
            {
                throw new NotImplementedException();
            }
        }

        public bool[] Status
        {
            get { return _status; }
            private set
            {
                _status = value;
                SubCount = _status.Length;
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private async void RunLoop()
        {
            byte[] lastData = null;
            while (true)
            {
                await Read(async reader =>
                {
                    var resp = await reader.Read<ReadStatusResponse>();
                    if (resp != null)
                    {
                        if (lastData != null && !resp.Data.SequenceEqual(lastData))
                        {
                            // Something changed
                            int swCount = Math.Min(resp.SwitchesCount, resp.Data.Length * 8);
                            var ret = new bool[swCount];
                            for (int i = 0; i < swCount; i++)
                            {
                                ret[i] = (resp.Data[i / 8] & (1 << (i % 8))) != 0;
                            }
                            Status = ret;
                        }
                        lastData = resp.Data;
                    }
                });
                await Task.Delay(PollPeriod);
            }
        }

        public event EventHandler StatusChanged;
    }
}