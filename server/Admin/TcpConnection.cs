using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Lucky.Home.Admin;
using Lucky.Home.Devices;
using Lucky.IO;

namespace Lucky.Home
{
    public class TcpConnection : Connection
    {
        private readonly TcpClient _client;
        private bool _connected;
        private readonly AdminInterface _adminInterface;

        public TcpConnection()
        {
            Connected = false;
            _adminInterface = new AdminInterface(this);
            _client = new TcpClient();
            Connect();
        }

        public async void Connect()
        {
            // Start listener
            await _client.ConnectAsync("localhost", Constants.DefaultAdminPort);
            await Task.Run(() => HandleConnected());
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

        private async void HandleConnected()
        {
            Connected = true;
            await FetchTree();
        }

        private async Task FetchTree()
        {
            var topology = await _adminInterface.GetTopology();
            var uinodes = topology.Select(n => new UiNode(n, null));
            App.Current.Dispatcher.Invoke(() => Nodes = new ObservableCollection<UiNode>(uinodes));
        }

        public async Task<bool> RenameNode(Node node, Guid newName)
        {
            bool ret = await _adminInterface.RenameNode(node.Address, node.Id, newName);
            await FetchTree();
            return ret;
        }

        public async Task<string[]> GetDevices()
        {
            return await _adminInterface.GetDeviceTypes();
        }

        public async Task<string> CreateDevice(Node node, string sinkId, string deviceType, string argument)
        {
            return await _adminInterface.CreateDevice(new SinkPath(node.Id, sinkId), deviceType, argument);
        }

        private class AdminInterface : IAdminInterface
        {
            private readonly TcpConnection _connection;
            private MessageChannel _channel;

            public AdminInterface(TcpConnection connection)
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

            public async Task<string[]> GetDeviceTypes()
            {
                return (string[])await Request();
            }

            public async Task<bool> RenameNode(string nodeAddress, Guid oldId, Guid newId)
            {
                return (bool) await Request("RenameNode", nodeAddress, oldId, newId);
            }

            public async Task<string> CreateDevice(SinkPath sinkPath, string deviceType, string argument)
            {
                return (string) await Request("CreateDevice", sinkPath, deviceType, argument);
            }
        }
    }
}