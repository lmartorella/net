using System.Windows;
using Lucky.Home.Admin;

namespace Lucky.Home
{
    public class RenamingNode : DependencyObject
    {
        public RenamingNode(Node node, int pos)
        {
            Name = node.Id.ToString();
            Node = node;
            Index = pos;
        }

        public static readonly DependencyProperty NameProperty = DependencyProperty.Register(
            "Name", typeof (string), typeof (RenamingNode), new PropertyMetadata(default(string)));

        public string Name
        {
            get { return (string) GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }

        public Node Node { get; private set; }
        public int Index { get; private set; }
    }
}