using Lucky.Home.Services;
using Lucky.Home.Simulator;
using System;
using System.IO;

namespace Lucky.Home.Views
{
    [MockSink("ANIN", "Analog Integrator")]
    public class AnalogIntegratorSinkView : ISinkMock
    {
        private ILogger Logger;

        public void Init(ISimulatedNode node)
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("IntegratorSink", node.Id.ToString());
            node.IdChanged += (o, e) => Logger.SubKey = node.Id.ToString();
        }

        public void Read(BinaryReader reader)
        {
            throw new System.NotImplementedException();
        }

        public void Write(BinaryWriter writer)
        {
            // Write current sensor factor
            // 1A = 1mA, on 39ohm = 39mV, sampled against 1.024V/1024 = 1/39 of the scale
            writer.Write((float)(1.0f / 39.0f));

            // Write 1A on 100 samples
            UInt16 count = 100;
            UInt32 data = (uint)(39 * count);

            writer.Write(data);
            writer.Write(count);
        }
    }
}
