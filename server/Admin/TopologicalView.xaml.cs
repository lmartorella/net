using System;
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

            CreateDeviceCommand = new UiCommand(() =>
            {
                if (SelectedSinks.Length > 0)
                {
                    CreateDevice(SelectedSinks);
                }
            }, () => SelectedSinks.Length > 0);

            TcpConnection.NodeSelectionChanged += (o, e) => UpdateMenuItems();
        }

        private SinkNode[] SelectedSinks
        {
            get
            {
                return TcpConnection.Nodes.OfType<SinkNode>().Where(sn => sn.IsSelected).ToArray();
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

        private TcpConnection TcpConnection
        {
            get
            {
                return ((dynamic)((FrameworkElement)Parent).DataContext).Connection;
            }
        }

        private async void EndRename(bool commit)
        {
            if (_inEditItem != null)
            {
                // Rename the node
                if (commit)
                {
                    Guid newId;
                    var inEditItem = _inEditItem;
                    _inEditItem = null;
                    inEditItem.InRename = false;
                    RenameCommand.RaiseCanExecuteChanged();
                    if (Guid.TryParse(inEditItem.Name, out newId))
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
                            inEditItem.Name = inEditItem.Node.Id.ToString();
                        }
                    }
                    else
                    {
                        inEditItem.Name = "Invalid GUID!";
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        inEditItem.Name = inEditItem.Node.Id.ToString();
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
                string err = await TcpConnection.CreateDevice(sinkPaths, wnd.DeviceType.TypeName, arguments);
                if (err != null)
                {
                    MessageBox.Show(err, "Error creating the device");
                }
            }
        }

        private object[] ParseArguments(string argumentStr, Type[] types)
        {
            return argumentStr.Split(',').Select((str, i) => Convert.ChangeType(str, types[i])).ToArray();
        }
    }
}
