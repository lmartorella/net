using Lucky.Home.Protocol;
using Lucky.Home.Services;
using System;

namespace Lucky.Home.Simulator
{
    class NodeBase : ISimulatedNodeInternal
    {
        private SimulatorNodesService.NodeData _nodeData;
        protected ILogger Logger { get; private set; }

        public NodeBase(string logKey, SimulatorNodesService.NodeData nodeData)
        {
            _nodeData = nodeData;
            nodeData.Id = nodeData.Id ?? new NodeId();
            Logger = Manager.GetService<ILoggerFactory>().Create(logKey, Id.ToString());
        }

        public Action Reset { get; set; }

        public NodeId Id
        {
            get
            {
                return _nodeData.Id;
            }
            set
            {
                _nodeData.Id = value;
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
                return _nodeData.Status;
            }
            set
            {
                _nodeData.Status = value;
                var svc = Manager.GetService<SimulatorNodesService>();
                svc.Save();
            }
        }

        public event EventHandler IdChanged;
    }
}
