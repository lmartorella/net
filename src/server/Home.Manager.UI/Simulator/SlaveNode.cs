using System;
using System.Linq;
using Lucky.Home.Services;

namespace Lucky.Home.Simulator
{
    class SlaveNode : NodeBase
    {
        public ISinkMock[] Sinks { get; }

        public SlaveNode(SimulatorNodesService.NodeData nodeData, string[] sinks)
            :base("SlaveNode", nodeData)
        {
            var sinkManager = Manager.GetService<MockSinkManager>();
            Sinks = sinks.Select(name => sinkManager.Create(name, this)).ToArray();
        }
    }
}
