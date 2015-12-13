using System.IO;

namespace Lucky.HomeMock.Sinks
{
    class SwitchArraySink : SinkMockBase
    {
        private readonly MainWindow _owner;

        public SwitchArraySink(MainWindow owner) 
            :base("SWAR")
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
                writer.Write((ushort)count);
                int bytes = (count / 8) + 1;
                writer.Write((ushort)bytes);

                byte[] ret = new byte[bytes];
                for (int i = 0; i < count; i++)
                {
                    // Pack bits
                    ret[i / 8] = (byte)(ret[i / 8] | (_owner.Switches[i].Value ? (1 << (i % 8)) : 0));
                }
                writer.Write(ret);
            });
        }
    }
}
