using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Lucky.Home.Design;

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
                UiNode node = TreeView.SelectedItem as UiNode;
                if (node != null)
                {
                    int pos;
                    ObservableCollection<object> collection = Connection.Nodes;
                    if (FindNode(node, out pos, ref collection))
                    {
                        collection.RemoveAt(pos);
                        _inEditItem = new RenamingNode(node, pos, collection);
                        collection.Insert(pos, _inEditItem);
                    }
                }
            }, () =>
            {
                object node = TreeView.SelectedItem;
                return node is UiNode;
            });

            Connection = new TcpConnection();

            //Connection = new SampleData1();
        }

        private bool FindNode(UiNode node, out int pos, ref ObservableCollection<object> collection)
        {
            pos = collection.IndexOf(node);
            if (pos < 0)
            {
                // Find children
                foreach (var child in collection.OfType<UiNode>())
                {
                    collection = child.Children;
                    if (FindNode(node, out pos, ref collection))
                    {
                        return true;
                    }
                }
            }
            else
            {
                return true;
            }
            return false;
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

        private async void EndRename(bool commit)
        {
            if (_inEditItem == null)
            {
                return;
            }

            _inEditItem.Parent.RemoveAt(_inEditItem.Index);
            _inEditItem.Parent.Insert(_inEditItem.Index, _inEditItem.Node);

            // Rename the node
            if (commit)
            {
                var newId = new Guid(_inEditItem.Name);
                await Connection.RenameNode(_inEditItem.Node.Node, newId);
                _inEditItem.Node.Id = newId;
            }
            _inEditItem = null;
        }
    }
}
