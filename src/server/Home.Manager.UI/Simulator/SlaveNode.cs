namespace Lucky.Home.Simulator
{
    class SlaveNode : NodeBase
    {
        public SlaveNode(SimulatorNodesService.NodeData nodeData)
            :base("SlaveNode", nodeData)
        { }

        public override void Reset()
        {
        }
    }
}
