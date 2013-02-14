using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using Lucky.HomeMock.Core;
using Lucky.HomeMock.Sinks;

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

        private const int DisplayPort = 18000;
        private Display _displayModel;

        public MainWindow()
        {
            InitializeComponent();

            EnterInitState();
            _displayModel = new Display(DisplayPort);
            _displayModel.Data += (o, args) =>
                            {
                                Dispatcher.Invoke((Action)(() =>
                                {
                                    DisplayBox.Text += args.Str + Environment.NewLine;
                                }));
                            };
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
                                    bool exc = false;
                                    try
                                    {
                                        // Write RGST command header
                                        writer.Write(ASCIIEncoding.ASCII.GetBytes("RGST"));
                                        // Num of peers
                                        writer.Write((short)1);

                                        // Peer device ID
                                        writer.Write(_displayModel.DeviceID);
                                        // Peer device capatibilities
                                        writer.Write(_displayModel.DeviceCaps);
                                        // PORT
                                        writer.Write(_displayModel.Port);
                                    }
                                    catch (IOException)
                                    {
                                        exc = true;
                                    }

                                    // Read response
                                    int errCode = 0;
                                    try
                                    {
                                        errCode = reader.ReadInt16();
                                    }
                                    catch (IOException)
                                    { }
                                    if (errCode == 0 && exc)
                                    { 
                                        errCode = -1; 
                                    }
                                    Dispatcher.BeginInvoke((Action)(() => LogBox.AppendText("Error code: " + errCode + "\n")), null);
                                    if (errCode == 2)
                                    {
                                        // Assign new GUID!
                                        Data.DeviceId = new Guid(reader.ReadBytes(16));
                                        Dispatcher.Invoke((Action)(() => LogBox.AppendText("New GUID: " + Data.DeviceId + "\n")), null);
                                        if (Data.DeviceId != Guid.Empty)
                                        {
                                            // Stop HELO packets
                                            HeloSender = null;
                                        }
                                    }
                                }
                            }
                        }
                    }, null);
        }
    }
}
