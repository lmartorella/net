using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

// ReSharper disable MemberCanBePrivate.Global

namespace Lucky.Home
{
    /// <summary>
    /// Interaction logic for TopologicalView.xaml
    /// </summary>
    public partial class TopologicalView
    {
        private UiNode _inEditItem;

        public static readonly DependencyProperty NodesProperty = DependencyProperty.Register(
            "Nodes", typeof (ObservableCollection<UiNode>), typeof (TopologicalView), new PropertyMetadata(default(ObservableCollection<UiNode>)));

        public ObservableCollection<UiNode> Nodes
        {
            get { return (ObservableCollection<UiNode>) GetValue(NodesProperty); }
            set { SetValue(NodesProperty, value); }
        }

        public static readonly DependencyProperty RenameCommandProperty = DependencyProperty.Register(
            "RenameCommand", typeof(UiCommand), typeof(TopologicalView), new PropertyMetadata(default(ICommand)));

        public UiCommand RenameCommand
        {
            get { return (UiCommand)GetValue(RenameCommandProperty); }
            set { SetValue(RenameCommandProperty, value); }
        }

        public static readonly DependencyProperty ResetCommandProperty = DependencyProperty.Register(
            "ResetCommand", typeof(UiCommand), typeof(TopologicalView), new PropertyMetadata(default(ICommand)));

        public UiCommand ResetCommand
        {
            get { return (UiCommand)GetValue(ResetCommandProperty); }
            set { SetValue(ResetCommandProperty, value); }
        }

        public static readonly DependencyProperty CreateDeviceCommandProperty = DependencyProperty.Register(
            "CreateDeviceCommand", typeof (UiCommand), typeof (TopologicalView), new PropertyMetadata(default(UiCommand)));

        public UiCommand CreateDeviceCommand
        {
            get { return (UiCommand) GetValue(CreateDeviceCommandProperty); }
            set { SetValue(CreateDeviceCommandProperty, value); }
        }

        public TopologicalView()
        {
            InitializeComponent();
            TreeView.DataContext = this;

            RenameCommand = new UiCommand(() =>
            {
                if (SelectedUiNode != null)
                {
                    _inEditItem = SelectedUiNode;
                    _inEditItem.InRename = true;
                    RenameCommand.RaiseCanExecuteChanged();
                }
            }, () => SelectedUiNode != null && !SelectedUiNode.InRename);

            ResetCommand = new UiCommand(async () =>
            {
                if (SelectedUiNode != null)
                {
                    await TcpConnection.ResetNode(SelectedUiNode.Node);
                }
            }, () => SelectedUiNode != null);

            CreateDeviceCommand = new UiCommand(() =>
            {
                if (SelectedSinks.Length > 0)
                {
                    CreateDevice(SelectedSinks);
                }
            }, () => SelectedSinks.Length > 0);
        }

        private SinkNode[] SelectedSinks
        {
            get
            {
                return GetAllNodes<SinkNode>().Where(sn => sn.IsSelected).ToArray();
            }
        }

        private IEnumerable<T> GetAllNodes<T>(TreeNode root = null) where T: TreeNode
        {
            if (root != null)
            {
                return root.Children.OfType<T>().Concat(root.Children.OfType<TreeNode>().SelectMany(GetAllNodes<T>));
            }
            else
            {
                return TcpConnection.Nodes.SelectMany(GetAllNodes<T>);
            }
        }

        private UiNode SelectedUiNode
        {
            get
            {
                return TreeView.SelectedItem as UiNode;
            }
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

        public static readonly DependencyProperty TcpConnectionProperty = DependencyProperty.Register(
            "TcpConnection", typeof (Connection), typeof (TopologicalView), new PropertyMetadata(null, HandleConnectionChanged));

        private static void HandleConnectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                ((Connection)e.NewValue).NodeSelectionChanged += (o, e1) =>
                {
                    ((TopologicalView) d).UpdateMenuItems();
                };
            }
        }

        public Connection TcpConnection
        {
            get { return (Connection) GetValue(TcpConnectionProperty); }
            set { SetValue(TcpConnectionProperty, value); }
        }

        private async void EndRename(bool commit)
        {
            if (_inEditItem != null)
            {
                // Rename the node
                if (commit)
                {
                    NodeId newId;
                    var inEditItem = _inEditItem;
                    _inEditItem = null;
                    inEditItem.InRename = false;
                    RenameCommand.RaiseCanExecuteChanged();
                    if (NodeId.TryParse(inEditItem.Name, out newId))
                    {
                        inEditItem.Name = "Renaming...";
                        if (await TcpConnection.RenameNode(inEditItem.Node, newId))
                        {
                            inEditItem.Name = newId.ToString();
                        }
                        else
                        {
                            inEditItem.Name = "Error in connection!";
                            await Task.Delay(TimeSpan.FromSeconds(2));
                            inEditItem.Name = inEditItem.Node.NodeId.ToString();
                        }
                    }
                    else
                    {
                        inEditItem.Name = "Invalid GUID!";
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        inEditItem.Name = inEditItem.Node.NodeId.ToString();
                    }
                }
            }
        }

        private void HandleSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            UpdateMenuItems();
        }

        private void UpdateMenuItems()
        {
            RenameCommand.RaiseCanExecuteChanged();
            CreateDeviceCommand.RaiseCanExecuteChanged();
        }

        private async void CreateDevice(SinkNode[] sinks)
        {
            // Create device
            CreateDeviceWindow wnd = new CreateDeviceWindow();
            wnd.DeviceTypes = await TcpConnection.GetDeviceTypes();
            if (wnd.ShowDialog() == true)
            {
                var argumentTypes = wnd.DeviceType.ArgumentTypes.Select(Type.GetType).ToArray();
                object[] arguments = ParseArguments(wnd.Argument, argumentTypes);
                var sinkPaths = sinks.Select(n => n.SinkPath).ToArray();
                string err = await TcpConnection.CreateDevice(sinkPaths, wnd.DeviceType.Name, arguments);
                if (err != null)
                {
                    MessageBox.Show(err, "Error creating the device");
                }
            }
        }

        private object[] ParseArguments(string argumentStr, Type[] types)
        {
            if (string.IsNullOrEmpty(argumentStr))
            {
                return new object[0];
            }
            else
            {
                return argumentStr.Split(',').Select((str, i) => Convert.ChangeType(str, types[i])).ToArray();
            }
        }
    }
}
