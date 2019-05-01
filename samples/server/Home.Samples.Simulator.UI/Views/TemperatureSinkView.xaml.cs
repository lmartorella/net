using Lucky.Home.Services;
using Lucky.Home.Simulator;
using System;
using System.IO;
using System.Windows.Controls;

namespace Lucky.Home.Views
{
    [MockSink("TEMP", "Temp sensor")]
    public partial class TemperatureSinkView : UserControl, ISinkMock
    {
        private readonly Random _random = new Random();
        private ILogger Logger;

        public TemperatureSinkView()
        {
            InitializeComponent();
        }

        public void Init(ISimulatedNode node)
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("TempSink", node.Id.ToString());
            node.IdChanged += (o, e) => Logger.SubKey = node.Id.ToString();
        }

        public void Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        private static ushort FromDec(float n)
        {
            byte intPart = (byte)(sbyte)Math.Floor(n);
            byte decPart = (byte)((n - intPart) * 256.0f);
            return (ushort)((decPart << 8) + intPart);
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((byte)1);
            int hum = _random.Next(100);
            float temp = _random.Next(200, 300) / 10.0f;

            var hums = FromDec(hum);
            var temps = FromDec(temp);
            writer.Write(hums);
            writer.Write(temps);

            uint checksum = (uint)((hums & 0xff) + (hums >> 8));
            checksum += (uint)((temps & 0xff) + (temps >> 8));
            writer.Write((byte)checksum);
        }
    }
}
