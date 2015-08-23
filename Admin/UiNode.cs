using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Lucky.Home.Admin;

namespace Lucky.Home
{
    public class UiNode : DependencyObject
    {
        public Guid Id { get; set; }

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
            Id = node.Id;
            Status = node.Status;
            Children = new ObservableCollection<object>(node.Children.Select(n => new UiNode(n)));
        }
    }
}