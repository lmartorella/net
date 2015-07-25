using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using Lucky.Home.Admin;

namespace Lucky.Home
{
    public class Connection : DependencyObject
    {
        private readonly TcpClient _client;
        private bool _connected;
        private NetworkStream _stream;
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

        private void HandleConnected()
        {
            Connected = true;
            using (_stream = _client.GetStream())
            {
                Send(new GetTopologyMessage());
                var topology = Receive<GetTopologyMessage.Response>().Roots;
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
            new DataContractSerializer(message.GetType()).WriteObject(_stream, message);
            _stream.Flush();
        }

        private T Receive<T>()
        {
            return (T)new DataContractSerializer(typeof(T)).ReadObject(_stream);
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
