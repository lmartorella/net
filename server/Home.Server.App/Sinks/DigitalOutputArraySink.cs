using System;
using System.Threading.Tasks;
using Lucky.Serialization;

// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Global
#pragma warning disable 649

namespace Lucky.Home.Sinks
{
    /// <summary>
    /// Passive output array
    /// </summary>
    [SinkId("DOAR")]
    internal class DigitalOutputArraySink : SinkBase
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

        protected async override Task OnInitialize()
        {
            await base.OnInitialize();
            await Read(async reader =>
            {
                var resp = await reader.Read<ReadStatusResponse>();
                SubCount = resp.SwitchCount;
                _status = new bool[SubCount];
            });

            // Align ext data
            await UpdateValues(_status);
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
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                UpdateValues(value);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        private async Task UpdateValues(bool[] value)
        {
            if (SubCount <= 0)
            {
                return;
            }

            await Write(async writer =>
            {
                int swCount = Math.Min(SubCount, value.Length);
                var msg = new WriterStatusMessage();
                msg.SwitchCount = (ushort)swCount;
                msg.Data = new byte[(swCount - 1) / 8 + 1];
                for (int i = 0; i < swCount; i++)
                {
                    msg.Data[i / 8] |= (byte)((value[i] ? 1 : 0) << (i % 8));
                }
                await writer.Write(msg);
            });
        }
    }
}