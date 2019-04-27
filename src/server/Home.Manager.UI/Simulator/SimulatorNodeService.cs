using Lucky.Home.Protocol;
using Lucky.Home.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Lucky.Home.Simulator
{
    class SimulatorNodesService : ServiceBaseWithData<SimulatorNodesService.Data>
    {
        public MasterNode[] Restore()
        {
            // Restore the nodes
            var state = State;
            List<MasterNode> ret = new List<MasterNode>();
            foreach (NodeData nodeData in state.MasterNodes)
            {
                var master = CreateNewMasterNode(nodeData);
                ret.Add(master);
                foreach (NodeData slaveData in nodeData.Children)
                {
                    CreateSlaveNode(master, slaveData);
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

        class Node : IStateProviderInternal
        {
            private readonly NodeData _nodeData;
            private readonly SimulatorNodesService _owner;

            public Node(SimulatorNodesService owner, NodeData nodeData)
            {
                _nodeData = nodeData;
                _owner = owner;
            }

            public Guid Id
            {
                get
                {
                    return _nodeData.Id;
                }
                set
                {
                    _nodeData.Id = value;
                    _owner.Logger.Log("New guid: " + value);
                    _owner.Save();
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
                    _owner.Save();
                }
            }
        }

        private void Save()
        {

        }

        public MasterNode CreateNewMasterNode(string[] sinks)
        {
            NodeData data = new NodeData { Sinks = sinks };
            State.MasterNodes = State.MasterNodes.Concat(new[] { data }).ToArray();
            return CreateNewMasterNode(data);
        }

        private MasterNode CreateNewMasterNode(NodeData nodeData)
        {
            return new MasterNode(Logger, new Node(this, nodeData), nodeData.Sinks);
        }

        private SlaveNode CreateSlaveNode(MasterNode master, NodeData nodeData)
        {
            var slave = new SlaveNode(Logger, new Node(this, nodeData), nodeData.Sinks);
            master.AddChild(slave);
            return slave;
        }
    }
}
