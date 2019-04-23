using System.IO;

namespace Lucky.HomeMock.Sinks
{
    class DigitalInputArraySink : SinkMockBase
    {
        private readonly MainWindow _owner;

        public DigitalInputArraySink(MainWindow owner) 
            :base("DIAR")
        {
            _owner = owner;
        }

        public override void Read(BinaryReader reader)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(BinaryWriter writer)
        {
            // Write bare switch cound
            _owner.Dispatcher.Invoke(() =>
            {
                var count = _owner.SwitchesCount;
                writer.Write((byte)count);
                int bytes = ((count - 1) / 8) + 1;

                byte[] ret = new byte[bytes];
                for (int i = 0; i < count; i++)
                {
                    // Pack bits
                    ret[i / 8] = (byte)(ret[i / 8] | (_owner.Inputs[i].Value ? (1 << (i % 8)) : 0));
                }
                writer.Write(ret);
            });
        }
    }
}
