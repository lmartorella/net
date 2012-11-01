using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using Lucky.HomeMock.Core;

namespace wpfMock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HeloSender _heloSender;
        private HomeReceiver _homeReceiver;

        private IPEndPoint _serverEndPoint;

        public MainWindow()
        {
            InitializeComponent();

            EnterInitState();
        }

        private HeloSender HeloSender
        {
            get
            {
                return _heloSender;
            }
            set
            {
                if (_heloSender != null)
                {
                    _heloSender.Dispose();
                }
                _heloSender = value;
            }
        }

        private HomeReceiver HomeReceiver
        {
            get
            {
                return _homeReceiver;
            }
            set
            {
                if (_homeReceiver != null)
                {
                    _homeReceiver.Dispose();
                }
                _homeReceiver = value;
            }
        }

        private void EnterInitState()
        {
            HeloSender = new HeloSender();
            HeloSender.Sent += (o, e) => Dispatcher.Invoke((Action)(() => LogBox.AppendText("Helo sent\n")));
            HomeReceiver = new HomeReceiver();
            HomeReceiver.HomeFound += (o, e) => Dispatcher.Invoke((Action)(() => EnterHomeFound()));
        }

        private void EnterHomeFound()
        {
            _serverEndPoint = new IPEndPoint(HomeReceiver.HomeHost, HomeReceiver.HomePort);

            LogBox.AppendText("Found home: " + _serverEndPoint.Address + ":" + _serverEndPoint.Port + "\n");
            LogBox.AppendText("Connecting...\n");

            // Connect to the server to publish device capabilities
            TcpClient client = new TcpClient();
            client.BeginConnect(_serverEndPoint.Address, _serverEndPoint.Port, ar =>
                    {
                        client.EndConnect(ar);

                        using (Stream stream = client.GetStream())
                        {
                            // Now sends message
                            using (BinaryWriter writer = new BinaryWriter(stream))
                            {
                                using (BinaryReader reader = new BinaryReader(stream))
                                {
                                    // Write HELLO!
                                    byte[] msg = ASCIIEncoding.ASCII.GetBytes("HELLO!");
                                    writer.Write((short)msg.Length);
                                    writer.Write(msg);

                                    // Read response
                                    int l = reader.ReadInt16();
                                    byte[] buffer = reader.ReadBytes(l);
                                    Dispatcher.Invoke((Action)(() => LogBox.AppendText("Received: " + ASCIIEncoding.ASCII.GetString(buffer) + "\n")), null);
                                }
                            }
                        }
                    }, null);
        }
    }
}
