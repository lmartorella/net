using Lucky.Home.Protocol;
using Lucky.Home.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Threading;

namespace Lucky.Home.Simulator
{
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
                    var master = CreateNewMasterNode(dispatcher, nodeData, false);
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

        [DataContract]
        internal class Data
        {
            [DataMember]
            public NodeData[] MasterNodes { get; set; }
        }

        [DataContract]
        internal class NodeData
        {
            [DataMember]
            public NodeId Id { get; set; }

            [DataMember]
            public string[] Sinks { get; set; }

            [DataMember]
            public NodeStatus Status { get; set; }

            [DataMember]
            public NodeData[] Children { get; set; }
        }

        public MasterNode CreateNewMasterNode(Dispatcher dispatcher, string[] sinks)
        {
            NodeData data = new NodeData { Sinks = sinks };
            return CreateNewMasterNode(dispatcher, data, true);
        }

        private MasterNode CreateNewMasterNode(Dispatcher dispatcher, NodeData nodeData, bool save)
        {
            var node = new MasterNode(dispatcher, nodeData);
            if (save)
            {
                State.MasterNodes = (State.MasterNodes ?? new NodeData[0]).Concat(new[] { nodeData }).ToArray();
                Save();
            }
            return node;
        }

        public new void Save()
        {
            base.Save();
        }

        private SlaveNode CreateSlaveNode(MasterNode master, NodeData nodeData)
        {
            var slave = new SlaveNode(nodeData);
            master.AddChild(slave);
            return slave;
        }
    }
}
