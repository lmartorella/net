using System.Collections.ObjectModel;
using System.Windows;

namespace Lucky.Home
{
    public class RenamingNode : DependencyObject
    {
        public RenamingNode(UiNode node, int pos, ObservableCollection<object> parent)
        {
            Name = node.Id.ToString();
            Node = node;
            Index = pos;
            Parent = parent;
        }

        public static readonly DependencyProperty NameProperty = DependencyProperty.Register(
            "Name", typeof (string), typeof (RenamingNode), new PropertyMetadata(default(string)));

        public string Name
        {
            get { return (string) GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        internal UiNode Node { get; private set; }
        internal int Index { get; private set; }
        internal ObservableCollection<object> Parent { get; set; }

        public ObservableCollection<object> Children
        {
            get { return Node.Children; }
        } 
    }
}