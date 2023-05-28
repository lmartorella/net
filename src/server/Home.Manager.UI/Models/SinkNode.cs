using System.Windows;

namespace Lucky.Home.Models
{
    /// <summary>
    /// A sink node for the tree visualizer (always child of a node)
    /// </summary>
    class SinkNode : TreeNode
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

        public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
            "IsSelected", typeof (bool), typeof (SinkNode), new PropertyMetadata(false, HandleCheckedChanged));

        private static void HandleCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SinkNode me = (SinkNode) d;
            me.Root.RaiseSelectionChanged();
        }

        public bool IsSelected
        {
            get { return (bool) GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
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
    }
}