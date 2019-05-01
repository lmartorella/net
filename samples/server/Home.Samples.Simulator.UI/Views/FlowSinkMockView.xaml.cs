using Lucky.Home.Services;
using Lucky.Home.Simulator;
using System;
using System.IO;
using System.Threading;
using System.Windows.Controls;

namespace Lucky.Home.Views
{
    [MockSink("FLOW")]
    public partial class FlowSinkMockView : UserControl, ISinkMock
    {
        private Timer _timer;
        private uint _counter;
        private ushort _flow;
        private ILogger Logger;

        public FlowSinkMockView()
        {
            InitializeComponent();

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
