using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Lucky.Home.Admin;

namespace Lucky.Home
{
    public class UiNode : DependencyObject
    {
        public ObservableCollection<object> Children { get; set; }

        public NodeStatus Status { get; set; }
        internal Node Node { get; private set; }

        public UiNode()
        {
            Children = new ObservableCollection<object>();
        }

        internal UiNode(Node node)
        {
            Node = node;
            Name = node.Id.ToString();
            Status = node.Status;
            Children = new ObservableCollection<object>(node.Children.Select(n => new UiNode(n)));
        }

        public static readonly DependencyProperty InRenameProperty = DependencyProperty.Register(
            "InRename", typeof (bool), typeof (UiNode), new PropertyMetadata(default(bool)));

        public bool InRename
        {
            get { return (bool) GetValue(InRenameProperty); }
            set { SetValue(InRenameProperty, value); }
        }

        public static readonly DependencyProperty NameProperty = DependencyProperty.Register(
            "Name", typeof (string), typeof (UiNode), new PropertyMetadata(default(string)));

        public string Name
        {
            get { return (string) GetValue(NameProperty); }
            set { SetValue(NameProperty, value); }
        }
    }
}