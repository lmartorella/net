using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization;
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
        private MessageChannel _channel;

        public TcpConnection()
        {
            Connected = false;
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
                Dispatcher.Invoke(() => StatusText = _connected ? "Connected" : "Disconnected");
            }
        }

        private async void HandleConnected()
        {
            Connected = true;
            await FetchTree();
        }

        private async Task FetchTree()
        {
            try
            {
                using (_channel = new MessageChannel(_client.GetStream()))
                {
                    await Send(new Container {Message = new GetTopologyMessage()});
                    var response = (await Receive<GetTopologyMessage.Response>());
                    if (response == null)
                    {
                        Connected = false;
                    }
                    else
                    {
                        var topology = response.Roots;
                        var uinodes = topology.Select(n => new UiNode(n));
                        Dispatcher.Invoke(() => Nodes = new ObservableCollection<UiNode>(uinodes));
                    }
                }
            }
            catch (Exception)
            {
                Connected = false;
            }
        }

        private async Task Send<T>(T message)
        {
            using (var ms = new MemoryStream())
            {
                new DataContractSerializer(message.GetType()).WriteObject(ms, message);
                ms.Flush();
                await _channel.WriteMessage(ms.ToArray());
            }
        }

        private async Task<T> Receive<T>() where T: class
        {
            var buffer = await _channel.ReadMessage();
            if (buffer == null)
            {
                return null;
            }
            using (var ms = new MemoryStream(buffer))
            {
                return (T)new DataContractSerializer(typeof(T)).ReadObject(ms);
            }
        }

        public override async Task<bool> RenameNode(Node node, Guid newName)
        {
            try
            {
                bool retValue = false;
                using (_channel = new MessageChannel(_client.GetStream()))
                {
                    await Send(new Container { Message = new RenameNodeMessage(node, newName) });
                    retValue = (await Receive<RenameNodeMessage.Response>() != null);
                }
                await FetchTree();
                return retValue;
            }
            catch (Exception)
            {
                Connected = false;
                return false;
            }
        }

        public override async Task<string[]> GetDevices()
        {
            try
            {
                using (_channel = new MessageChannel(_client.GetStream()))
                {
                    await Send(new Container { Message = new GetDeviceTypesMessage() });
                    return (await Receive<GetDeviceTypesMessage.Response>()).DeviceTypes;
                }
            }
            catch (Exception)
            {
                Connected = false;
                return new string[0];
            }
        }

        public override async Task<string> CreateDevice(Node node, string sinkId, string deviceType, string argument)
        {
            try
            {
                using (_channel = new MessageChannel(_client.GetStream()))
                {
                    await Send(new Container { Message = new CreateDeviceMessage { SinkPath = new SinkPath(node.Id, sinkId), DeviceType = deviceType, Argument = argument } });
                    return (await Receive<CreateDeviceMessage.Response>()).Error;
                }
            }
            catch (Exception exc)
            {
                Connected = false;
                return exc.Message;
            }
        }
    }
}