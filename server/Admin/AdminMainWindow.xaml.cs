using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Lucky.Home.Admin;

namespace Lucky.Home
{
    /// <summary>
    /// Interaction logic for AdminMainWindow.xaml
    /// </summary>
    public partial class AdminMainWindow
    {
        private UiNode _inEditItem;

        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register(
            "Connection", typeof (Connection), typeof (AdminMainWindow), new PropertyMetadata(default(Connection)));

        public Connection Connection
        {
            get { return (Connection) GetValue(ConnectionProperty); }
            set { SetValue(ConnectionProperty, value); }
        }

        public static readonly DependencyProperty RenameCommandProperty = DependencyProperty.Register(
            "RenameCommand", typeof(UiCommand), typeof(AdminMainWindow), new PropertyMetadata(default(ICommand)));

        public UiCommand RenameCommand
        {
            get { return (UiCommand)GetValue(RenameCommandProperty); }
            set { SetValue(RenameCommandProperty, value); }
        }

        public static readonly DependencyProperty CreateDeviceCommandProperty = DependencyProperty.Register(
            "CreateDeviceCommand", typeof (UiCommand), typeof (AdminMainWindow), new PropertyMetadata(default(UiCommand)));

        public UiCommand CreateDeviceCommand
        {
            get { return (UiCommand) GetValue(CreateDeviceCommandProperty); }
            set { SetValue(CreateDeviceCommandProperty, value); }
        }

        public AdminMainWindow()
        {
            InitializeComponent();
            DataContext = this;

            RenameCommand = new UiCommand(() =>
            {
                if (SelectedNode != null)
                {
                    _inEditItem = SelectedNode;
                    SelectedNode.InRename = true;
                    RenameCommand.RaiseCanExecuteChanged();
                }
            }, () => SelectedNode != null && !SelectedNode.InRename);

            CreateDeviceCommand = new UiCommand(() =>
            {
                if (SelectedNode != null)
                {
                    CreateDevice(SelectedNode.Node);
                }
            }, () => SelectedNode != null && !SelectedNode.InRename);

            Connection = new TcpConnection();

            //Connection = new SampleData1();
        }

        private UiNode SelectedNode
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
                        if (await Connection.RenameNode(inEditItem.Node, newId))
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
            RenameCommand.RaiseCanExecuteChanged();
            CreateDeviceCommand.RaiseCanExecuteChanged();
        }

        private async void CreateDevice(Node node)
        {
            CreateDeviceWindow wnd = new CreateDeviceWindow();
            wnd.Sinks = node.Sinks;
            wnd.DeviceTypes = await Connection.GetDevices();
            if (wnd.ShowDialog() == true)
            {
                // Create device
                string err = await Connection.CreateDevice(node, wnd.SinkId, wnd.DeviceType, wnd.Argument);
                if (err != null)
                {
                    MessageBox.Show(err, "Error creating the device");
                }
            }
        }
    }
}
