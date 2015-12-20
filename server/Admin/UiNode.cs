using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Lucky.Home.Admin;

namespace Lucky.Home
{
    public class UiNode : TreeNode
    {
        // If a tcp node...
        public NodeStatus Status { get; set; }
        // If a tcp node...
        internal Node Node { get; private set; }

        /// <summary>
        /// From a TCP node
        /// </summary>
        internal UiNode(Node node, UiNode parent)
        {
            Node = node;
            Parent = parent;
            Name = node.Id.ToString();
            Status = node.Status;
            Children = new ObservableCollection<object>(node.Children.Select(n => new UiNode(n, this)));
            SinkNames = string.Join(", ", node.Sinks);

            foreach (var sink in node.Sinks)
            {
                Children.Add(new SinkNode(sink, this));
            }
        }

        public static readonly DependencyProperty InRenameProperty = DependencyProperty.Register(
            "InRename", typeof (bool), typeof (UiNode), new PropertyMetadata(default(bool)));

        public bool InRename
        {
            get { return (bool) GetValue(InRenameProperty); }
            set { SetValue(InRenameProperty, value); }
        }

        public static readonly DependencyProperty SinkNamesProperty = DependencyProperty.Register(
            "SinkNames", typeof (string), typeof (UiNode), new PropertyMetadata(default(string)));

        public string SinkNames
        {
            get { return (string) GetValue(SinkNamesProperty); }
            set { SetValue(SinkNamesProperty, value); }
        }
    }
}