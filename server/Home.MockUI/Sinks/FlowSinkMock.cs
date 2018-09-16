using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Lucky.HomeMock.Sinks
{
    class FlowSinkMock : SinkMockBase
    {
        private Timer _timer;
        private uint _counter;
        private ushort _flow;

        public FlowSinkMock()
            : base("FLOW")
        {
            // 10 lt min
            _flow = 55;

            _timer = new Timer(o =>
            {
                _counter += _flow; 
            }, null, 0, 1000);
        }

        public override void Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public override void Write(BinaryWriter writer)
        {
            writer.Write(_counter);
            writer.Write(_flow);
        }
    }
}
