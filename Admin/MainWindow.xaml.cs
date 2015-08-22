using System;
using System.Windows;
using System.Windows.Input;
using Lucky.Home.Admin;

namespace Lucky.Home
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register(
            "Connection", typeof (Connection), typeof (MainWindow), new PropertyMetadata(default(Connection)));

        public Connection Connection
        {
            get { return (Connection) GetValue(ConnectionProperty); }
            set { SetValue(ConnectionProperty, value); }
        }

        public static readonly DependencyProperty RenameCommandProperty = DependencyProperty.Register(
            "RenameCommand", typeof (ICommand), typeof (MainWindow), new PropertyMetadata(default(ICommand)));

        private RenamingNode _inEditItem;

        public ICommand RenameCommand
        {
            get { return (ICommand) GetValue(RenameCommandProperty); }
            set { SetValue(RenameCommandProperty, value); }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            RenameCommand = new UiCommand(() =>
            {
                object node = TreeView.SelectedItem;
                int pos = Connection.Nodes.IndexOf(node);
                Connection.Nodes.Remove(node);
                _inEditItem = new RenamingNode((Node)node, pos);
                Connection.Nodes.Insert(pos, _inEditItem);
            }, () =>
            {
                object node = TreeView.SelectedItem;
                return node is Node;
            });

            //Connection = new TcpConnection();

            Connection = new Design.SampleData1();
        }

        private void RenameBoxKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    EndRename(true);
                    break;
                case Key.Escape:
                    EndRename(false);
                    break;
            }
        }

        private void RenameBoxLostFocus(object sender, RoutedEventArgs e)
        {
            EndRename(true);
        }

        private void EndRename(bool commit)
        {
            if (_inEditItem == null)
            {
                return;
            }

            Connection.Nodes.RemoveAt(_inEditItem.Index);
            Connection.Nodes.Insert(_inEditItem.Index, _inEditItem.Node);

            // Rename the node
            if (commit)
            {
                Connection.RenameNode(_inEditItem.Node, new Guid(_inEditItem.Name));
            }
            _inEditItem = null;
        }
    }
}
