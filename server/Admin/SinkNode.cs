using Lucky.Home.Devices;

namespace Lucky.Home
{
    public class SinkNode : TreeNode
    {
        public SinkNode(string name, int subCount, TreeNode parent)
        {
            Name = name;
            Parent = parent;

            for (int i = 0; i < subCount; i++)
            {
                Children.Add(new SinkNode(i.ToString(), 0, this));
            }
        }

        private UiNode ParentUiNode
        {
            get
            {
                return (Parent is UiNode) ? (UiNode)Parent : ((SinkNode) Parent).ParentUiNode;
            }
        }

        private SinkNode ParentSink
        {
            get
            {
                return (Parent is SinkNode) ? ((SinkNode)Parent).ParentSink : this;
            }
        }

        private int SubIndex
        {
            get
            {
                return (Parent is SinkNode) ? int.Parse(Name) : -1;
            }
        }

        public SinkPath SinkPath
        {
            get
            {
                return new SinkPath(ParentUiNode.Node.Id, ParentSink.Name, SubIndex);
            }
        }
    }
}