using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;
using Lucky.Home.Admin;
using Lucky.Home.Devices;

// ReSharper disable RedundantArgumentDefaultValue

namespace Lucky.Home
{
    public class Connection : DependencyObject, IDisposable
    {
        private readonly TcpClient _client;
        private bool _connected;
        private readonly AdminClient _adminInterface;

        public Connection(Action connectedHandler)
        {
            Nodes = new ObservableCollection<UiNode>();
            Devices = new ObservableCollection<UiDevice>();

            Connected = false;
            _adminInterface = new AdminClient(() => _client.GetStream(), () => Connected = false);
            _client = new TcpClient();
            Connect(connectedHandler);
        }

        public static readonly DependencyProperty StatusTextProperty = DependencyProperty.Register(
            "StatusText", typeof (string), typeof (Connection), new PropertyMetadata(default(string)));

        public string StatusText
        {
            get { return (string) GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }

        public static readonly DependencyProperty NodesProperty = DependencyProperty.Register(
            "Nodes", typeof (ObservableCollection<UiNode>), typeof (Connection), new PropertyMetadata(default(ObservableCollection<UiNode>)));

        public ObservableCollection<UiNode> Nodes
        {
            get { return (ObservableCollection<UiNode>) GetValue(NodesProperty); }
            set { SetValue(NodesProperty, value); }
        }

        public static readonly DependencyProperty DevicesProperty = DependencyProperty.Register(
            "Devices", typeof (ObservableCollection<UiDevice>), typeof (Connection), new PropertyMetadata(default(ObservableCollection<UiDevice>)));

        public ObservableCollection<UiDevice> Devices
        {
            get { return (ObservableCollection<UiDevice>) GetValue(DevicesProperty); }
            set { SetValue(DevicesProperty, value); }
        }

        private bool Connected
        {
            get
            {
                return _connected;
            }
            set
            {
                _connected = value;
                App.Current.Dispatcher.Invoke(() => StatusText = _connected ? "Connected" : "Disconnected");
            }
        }

        public void Dispose()
        {
            _client.Close();
        }

        private async void Connect(Action connectedHandler)
        {
            // Start listener
            await _client.ConnectAsync("localhost", Constants.DefaultAdminPort);
            await FetchTree();
            await FetchDevices();
            Connected = true;
            App.Current.Dispatcher.Invoke(connectedHandler);
        }

        private async Task FetchTree()
        {
            var topology = await _adminInterface.GetTopology();
            var uinodes = topology.Select(n =>
            {
                var node = new UiNode(n, null);
                node.SelectionChanged += (o, e) =>
                {
                    if (NodeSelectionChanged != null)
                    {
                        NodeSelectionChanged(this, e);
                    }
                };
                return node;
            });
            App.Current.Dispatcher.Invoke(() => Nodes = new ObservableCollection<UiNode>(uinodes));
        }

        private async Task FetchDevices()
        {
            var devices = (await _adminInterface.GetDevices()).Select(desc => new UiDevice(desc));
            App.Current.Dispatcher.Invoke(() => Devices = new ObservableCollection<UiDevice>(devices));
        }

        public async Task<bool> RenameNode(Node node, Guid newName)
        {
            bool ret = await _adminInterface.RenameNode(node.Address, node.Id, newName);
            await FetchTree();
            return ret;
        }

        public async Task<DeviceTypeDescriptor[]> GetDeviceTypes()
        {
            return await _adminInterface.GetDeviceTypes();
        }

        public async void DeleteDevice(UiDevice uiDevice)
        {
            await _adminInterface.DeleteDevice(uiDevice.Id);
        }

        public async Task<string> CreateDevice(SinkPath[] sinks, string deviceTypeName, object[] arguments)
        {
            var descriptor = new DeviceDescriptor
            {
                SinkPaths = sinks,
                DeviceTypeName = deviceTypeName,
                Arguments = arguments
            };
            return await _adminInterface.CreateDevice(descriptor);
        }

        public event EventHandler NodeSelectionChanged;
    }
}
