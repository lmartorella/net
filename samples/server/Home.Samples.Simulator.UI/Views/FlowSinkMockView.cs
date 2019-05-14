using Lucky.Home.Services;
using Lucky.Home.Simulator;
using System;
using System.IO;
using System.Threading;

namespace Lucky.Home.Views
{
    /// <summary>
    /// Mock sink for flow meter 
    /// </summary>
    [MockSink("FLOW", "Flow meter")]
    public partial class FlowSinkMockView : ISinkMock
    {
        private Timer _timer;
        private uint _counter;
        private ushort _flow;
        private ILogger Logger;

        public FlowSinkMockView()
        {
            // 10 lt min
            _flow = 55;

            _timer = new Timer(o =>
            {
                _counter += _flow;
            }, null, 0, 1000);
        }

        public void Init(ISimulatedNode node)
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("FlowSink", node.Id.ToString());
            node.IdChanged += (o, e) => Logger.SubKey = node.Id.ToString();
        }

        public void Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(_counter);
            writer.Write(_flow);
        }
    }
}
