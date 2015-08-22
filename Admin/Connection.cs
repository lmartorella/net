using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using Lucky.Home.Admin;

namespace Lucky.Home
{
    public abstract class Connection : DependencyObject
    {
        protected Connection()
        {
            Nodes = new ObservableCollection<object>();
        }

        #region Dependency properties

        public static readonly DependencyProperty StatusTextProperty = DependencyProperty.Register(
            "StatusText", typeof(string), typeof(Connection));

        public string StatusText
        {
            get { return (string)GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }

        #endregion

        public static readonly DependencyProperty NodesProperty = DependencyProperty.Register(
            "Nodes", typeof(ObservableCollection<object>), typeof(Connection));

        public ObservableCollection<object> Nodes
        {
            get { return (ObservableCollection<object>)GetValue(NodesProperty); }
            set { SetValue(NodesProperty, value); }
        }

        public abstract Task RenameNode(Node node, Guid newName);
    }
}
