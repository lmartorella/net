using System;
using Lucky.Home.Serialization;

// ReSharper disable NotAccessedField.Local
// ReSharper disable UnusedMember.Global
#pragma warning disable 649

namespace Lucky.Home.Sinks.App
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

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Read(reader =>
            {
                var resp = reader.Read<ReadStatusResponse>();
                SubCount = resp.SwitchCount;
                _status = new bool[SubCount];
            });

            // Align ext data
            UpdateValues(_status);
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
                UpdateValues(value);
            }
        }

        private void UpdateValues(bool[] value)
        {
            if (SubCount <= 0)
            {
                return;
            }

            Write(writer =>
            {
                int swCount = Math.Min(SubCount, value.Length);
                var msg = new WriterStatusMessage();
                msg.SwitchCount = (ushort)swCount;
                msg.Data = new byte[(swCount - 1) / 8 + 1];
                for (int i = 0; i < swCount; i++)
                {
                    msg.Data[i / 8] |= (byte)((value[i] ? 1 : 0) << (i % 8));
                }
                writer.Write(msg);
            });
        }
    }
}