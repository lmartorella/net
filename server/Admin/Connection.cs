using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Lucky.Home.Admin;
using Lucky.Home.Devices;
using Lucky.IO;

// ReSharper disable RedundantArgumentDefaultValue

namespace Lucky.Home
{
    public class Connection : DependencyObject, IDisposable
    {
        private readonly TcpClient _client;
        private bool _connected;
        private readonly AdminInterface _adminInterface;

        public Connection(Action connectedHandler)
        {
            Nodes = new ObservableCollection<UiNode>();
            Devices = new ObservableCollection<UiDevice>();

            Connected = false;
            _adminInterface = new AdminInterface(this);
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

        public async Task<string> CreateDevice(SinkPath[] sinks, string deviceType, object[] arguments)
        {
            var descriptor = new DeviceDescriptor
            {
                SinkPaths = sinks,
                DeviceType = deviceType,
                Arguments = arguments
            };
            return await _adminInterface.CreateDevice(descriptor);
        }

        private class AdminInterface : IAdminInterface
        {
            private readonly Connection _connection;
            private MessageChannel _channel;

            public AdminInterface(Connection connection)
            {
                _connection = connection;
            }

            private async Task<object> Request([CallerMemberName] string methodName = null, params object[] arguments)
            {
                MessageRequest request = new MessageRequest { Method = methodName, Arguments = arguments };
                try
                {
                    using (_channel = new MessageChannel(_connection._client.GetStream()))
                    {
                        await Send(request);
                        return (await Receive()).Value;
                    }
                }
                catch (Exception)
                {
                    _connection.Connected = false;
                    return null;
                }
            }

            private async Task Send(MessageRequest message)
            {
                using (var ms = new MemoryStream())
                {
                    MessageRequest.DataContractSerializer.WriteObject(ms, message);
                    ms.Flush();
                    await _channel.WriteMessage(ms.ToArray());
                }
            }

            private async Task<MessageResponse> Receive()
            {
                var buffer = await _channel.ReadMessage();
                if (buffer == null)
                {
                    return null;
                }
                using (var ms = new MemoryStream(buffer))
                {
                    return (MessageResponse)MessageResponse.DataContractSerializer.ReadObject(ms);
                }
            }

            public async Task<Node[]> GetTopology()
            {
                return (Node[]) await Request();
            }

            public async Task<DeviceTypeDescriptor[]> GetDeviceTypes()
            {
                return (DeviceTypeDescriptor[])await Request();
            }

            public async Task<bool> RenameNode(string nodeAddress, Guid oldId, Guid newId)
            {
                return (bool) await Request("RenameNode", nodeAddress, oldId, newId);
            }

            public async Task<string> CreateDevice(DeviceDescriptor descriptor)
            {
                return (string)await Request("CreateDevice", descriptor);
            }

            public async Task<DeviceDescriptor[]> GetDevices()
            {
                return (DeviceDescriptor[])await Request();
            }

            public async Task DeleteDevice(Guid id)
            {
                await Request("DeleteDevice", id);
            }
        }

        public event EventHandler NodeSelectionChanged;
    }
}
