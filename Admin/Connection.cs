using System.IO;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Lucky.Home.Admin;
using Lucky.Home.IO;

namespace Lucky.Home
{
    public class Connection : DependencyObject
    {
        private readonly TcpClient _client;
        private bool _connected;
        private MessageChannel _channel;
        private GetTopologyMessage.Node[] _topology;

        public Connection()
        {
            StatusText = "Connecting...";
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
                Dispatcher.Invoke(() => StatusText = "Connected");
            }
        }

        private async void HandleConnected()
        {
            Connected = true;
            using (_channel = new MessageChannel(_client.GetStream()))
            {
                Send(new Container { Message = new GetTopologyMessage() });
                var topology = (await Receive<GetTopologyMessage.Response>()).Roots;
                Dispatcher.Invoke(() => Topology = topology);
            }
        }

        public GetTopologyMessage.Node[] Topology
        {
            get
            {
                return _topology;
            }
            set
            {
                _topology = value;
            }
        }

        private void Send<T>(T message)
        {
            using (var ms = new MemoryStream())
            {
                new DataContractSerializer(message.GetType()).WriteObject(ms, message);
                ms.Flush();
                _channel.WriteMessage(ms.GetBuffer());
            }
        }

        private async Task<T> Receive<T>()
        {
            using (var ms = new MemoryStream(await _channel.ReadMessage()))
            {
                return (T)new DataContractSerializer(typeof(T)).ReadObject(ms);
            }
        }

        #region Dependency properties

        public static readonly DependencyProperty StatusTextProperty = DependencyProperty.Register(
            "StatusText", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string StatusText
        {
            get { return (string)GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }

        #endregion
    }
}
