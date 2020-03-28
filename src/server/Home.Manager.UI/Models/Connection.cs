using Lucky.Home.Admin;
using Lucky.Home.Devices;
using Lucky.Home.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace Lucky.Home.Models
{
    /// <summary>
    /// Model for connection status
    /// </summary>
    internal class Connection : DependencyObject, IDisposable
    {
        private readonly TcpClient _client;
        private bool _connected;
        private readonly IAdminInterface _adminInterface;

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
            // Connect to?
            var args = Environment.GetCommandLineArgs();
            var targetHost = "localhost";
            if (args.Length >= 2)
            {
                targetHost = args[1];
            }
            // Start listener
            await _client.ConnectAsync(targetHost, Constants.DefaultAdminPort);
            await FetchTree();
            await FetchDevices();
            Connected = true;
            Application.Current.Dispatcher.Invoke(connectedHandler);
        }

        private async Task<bool> FetchTree()
        {
            var topology = await _adminInterface.GetTopology();
            if (topology != null)
            {
                var uinodes = topology.Select(n =>
                {
                    var node = new UiNode(n, null);
                    node.SelectionChanged += (o, e) =>
                    {
                        NodeSelectionChanged?.Invoke(this, e);
                    };
                    return node;
                });
                Application.Current.Dispatcher.Invoke(() => Nodes = new ObservableCollection<UiNode>(uinodes));
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task<bool> FetchDevices()
        {
            var devices = (await _adminInterface.GetDevices())?.Select(desc => new UiDevice(desc));
            if (devices != null)
            { 
                Application.Current.Dispatcher.Invoke(() => Devices = new ObservableCollection<UiDevice>(devices));
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> RenameNode(Node node, NodeId newName)
        {
            bool ret = await _adminInterface.RenameNode(node.Address, node.NodeId, newName);
            if (ret)
            {
                ret = await FetchTree();
            }
            return ret;
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

        public async Task ResetNode(Node node)
        {
            await _adminInterface.ResetNode(node.NodeId, node.Address);
        }

        public event EventHandler NodeSelectionChanged;
    }
}
