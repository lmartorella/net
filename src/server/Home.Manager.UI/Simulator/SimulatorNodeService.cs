using Lucky.Home.Protocol;
using Lucky.Home.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

namespace Lucky.Home.Simulator
{
    class NodeBase : ISimulatedNodeInternal
    {
        private SimulatorNodesService.NodeData _nodeData;
        protected ILogger Logger { get; private set; }

        public NodeBase(string logKey, SimulatorNodesService.NodeData nodeData)
        {
            _nodeData = nodeData;
            Logger = Manager.GetService<ILoggerFactory>().Create(logKey, Id.ToString());
        }

        public Action Reset { get; set; }

        public Guid Id
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

    class SimulatorNodesService : ServiceBaseWithData<SimulatorNodesService.Data>
    {
        public MasterNode[] Restore(Dispatcher dispatcher)
        {
            // Restore the nodes
            var state = State;
            List<MasterNode> ret = new List<MasterNode>();
            if (state.MasterNodes != null)
            {
                foreach (NodeData nodeData in state.MasterNodes)
                {
                    var master = CreateNewMasterNode(dispatcher, nodeData);
                    ret.Add(master);
                    if (nodeData.Children != null)
                    {
                        foreach (NodeData slaveData in nodeData.Children)
                        {
                            CreateSlaveNode(master, slaveData);
                        }
                    }
                }
            }
            return ret.ToArray();
        }

        internal class Data
        {
            public NodeData[] MasterNodes { get; set; }
        }

        internal class NodeData
        {
            public Guid Id { get; set; }
            public string[] Sinks { get; set; }
            public NodeStatus Status { get; set; }
            public NodeData[] Children { get; set; }
        }

        public void Save()
        {

        }

        public MasterNode CreateNewMasterNode(Dispatcher dispatcher, string[] sinks)
        {
            NodeData data = new NodeData { Sinks = sinks };
            State.MasterNodes = (State.MasterNodes ?? new NodeData[0]).Concat(new[] { data }).ToArray();
            return CreateNewMasterNode(dispatcher, data);
        }

        private MasterNode CreateNewMasterNode(Dispatcher dispatcher, NodeData nodeData)
        {
            return new MasterNode(dispatcher, nodeData);
        }

        private SlaveNode CreateSlaveNode(MasterNode master, NodeData nodeData)
        {
            var slave = new SlaveNode(nodeData);
            master.AddChild(slave);
            return slave;
        }
    }
}
