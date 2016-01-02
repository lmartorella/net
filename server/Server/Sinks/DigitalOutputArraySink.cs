using System;
using System.Threading.Tasks;
using Lucky.Home.Serialization;

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Passive output array
    /// </summary>
    [SinkId("DOAR")]
    internal class DigitalOutputArraySink : SinkBase, IDigitalOutputArraySink
    {
        private bool[] _status;

        public DigitalOutputArraySink()
        {
            Status = new bool[0];
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class ReadStatusResponse
        {
            public ushort SwitchCount;
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class WriterStatusMessage
        {
            public ushort SwitchCount;

            /// <summary>
            /// Switch data in bytes 
            /// </summary>
            [SerializeAsDynArray]
            public byte[] Data;
        }

        protected override async void OnInitialize()
        {
            base.OnInitialize();
            await Read(reader =>
            {
                var resp = reader.Read<ReadStatusResponse>();
                SubCount = resp.SwitchCount;
            });
            // Align ext data
            await UpdateValues();
        }

        public bool[] Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                UpdateValues();
            }
        }

        private async Task UpdateValues()
        {
            if (SubCount <= 0)
            {
                return;
            }

            await Write(writer =>
            {
                int swCount = Math.Min(SubCount, _status.Length);
                var msg = new WriterStatusMessage();
                msg.SwitchCount = (ushort)swCount;
                msg.Data = new byte[(swCount - 1) / 8 + 1];
                for (int i = 0; i < swCount; i++)
                {
                    msg.Data[i / 8] |= (byte)((_status[i] ? 1 : 0) << (i % 8));
                }
                writer.Write(msg);
            });
        }
    }
}