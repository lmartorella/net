using System.IO;

namespace Lucky.HomeMock.Sinks
{
    class DigitalOutputArraySink : SinkMockBase
    {
        private readonly MainWindow _owner;

        public DigitalOutputArraySink(MainWindow owner)
            : base("DOAR")
        {
            _owner = owner;
        }

        public override void Read(BinaryReader reader)
        {
            int switchesCount = reader.ReadByte();
            var byteCount = (switchesCount - 1) / 8 + 1;
            byte[] data = reader.ReadBytes(byteCount);

            // Write bare switch cound
            _owner.Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < switchesCount; i++)
                {
                    // Pack bits
                    _owner.Outputs[i].Value = (data[i / 8] & (1 << (i % 8))) != 0;
                }
            });
        }

        public override void Write(BinaryWriter writer)
        {
            // Write bare switch cound
            _owner.Dispatcher.Invoke(() =>
            {
                var count = _owner.SwitchesCount;
                writer.Write((ushort)count);
            });
        }
    }
}