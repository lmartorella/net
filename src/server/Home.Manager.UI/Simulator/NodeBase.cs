using Lucky.Home.Protocol;
using Lucky.Home.Services;
using System;
using System.Linq;

namespace Lucky.Home.Simulator
{
    abstract class NodeBase : ISimulatedNodeInternal
    {
        public SimulatorNodesService.NodeData NodeData { get; private set; }
        protected ILogger Logger { get; private set; }
        public ISinkMock[] Sinks { get; private set; }

        public NodeBase(string logKey, SimulatorNodesService.NodeData nodeData)
        {
            NodeData = nodeData;
            nodeData.Id = nodeData.Id ?? new NodeId();
            Logger = Manager.GetService<ILoggerFactory>().Create(logKey, Id.ToString());

            var sinkManager = Manager.GetService<MockSinkManager>();
            Sinks = nodeData.Sinks.Select(name => sinkManager.Create(name, this)).ToArray();
        }

        public abstract void Reset();

        public NodeId Id
        {
            get
            {
                return NodeData.Id;
            }
            set
            {
                NodeData.Id = value;
                Logger.Log("New guid: " + value);
                Logger.SubKey = value.ToString();

                var svc = Manager.GetService<SimulatorNodesService>();
                svc.Save();
                IdChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public NodeStatus Status
        {
            get
            {
                return NodeData.Status;
            }
            set
            {
                NodeData.Status = value;
                var svc = Manager.GetService<SimulatorNodesService>();
                svc.Save();
            }
        }

        public event EventHandler IdChanged;
    }
}
