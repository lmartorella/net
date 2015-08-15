using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using Lucky.Home.Admin;
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
            try
            {
                using (_channel = new MessageChannel(_client.GetStream()))
                {
                    await Send(new Container {Message = new GetTopologyMessage()});
                    var topology = (await Receive<GetTopologyMessage.Response>()).Roots;
                    Dispatcher.Invoke(() => Nodes = new ObservableCollection<Node>(topology));
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
                await _channel.WriteMessage(ms.GetBuffer());
            }
        }

        private async Task<T> Receive<T>()
        {
            using (var ms = new MemoryStream(await _channel.ReadMessage()))
            {
                return (T)new DataContractSerializer(typeof(T)).ReadObject(ms);
            }
        }
    }

    public class Connection : DependencyObject
    {
        public Connection()
        {
            Nodes = new ObservableCollection<Node>();
        }

        #region Dependency properties

        public static readonly DependencyProperty StatusTextProperty = DependencyProperty.Register(
            "StatusText", typeof(string), typeof(Connection));

        public string StatusText
        {
            get { return (string)GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }

        #endregion

        public static readonly DependencyProperty NodesProperty = DependencyProperty.Register(
            "Nodes", typeof(ObservableCollection<Node>), typeof(Connection));

        public ObservableCollection<Node> Nodes
        {
            get { return (ObservableCollection<Node>)GetValue(NodesProperty); }
            set { SetValue(NodesProperty, value); }
        }
    }
}
