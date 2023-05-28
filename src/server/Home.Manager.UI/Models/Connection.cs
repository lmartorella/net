using Lucky.Home.Admin;
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
            var targetHost = Manager.GetService<IConfigurationService>().GetConfig("host") ?? "localhost";
            // Start listener
            await _client.ConnectAsync(targetHost, Constants.DefaultAdminPort);
            await FetchTree();
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

        public async Task<bool> RenameNode(Node node, NodeId newName)
        {
            bool ret = await _adminInterface.RenameNode(node.Address, node.NodeId, newName);
            if (ret)
            {
                ret = await FetchTree();
            }
            return ret;
        }

        public async Task ResetNode(Node node)
        {
            await _adminInterface.ResetNode(node.NodeId, node.Address);
        }

        public event EventHandler NodeSelectionChanged;
    }
}
