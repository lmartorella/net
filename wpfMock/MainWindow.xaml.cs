using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Lucky.HomeMock.Core;
using Lucky.HomeMock.Sinks;

namespace Lucky.HomeMock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private HeloSender _heloSender;
        private ControlPortListener _controlPort;
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

        private ControlPortListener ControlPort
        {
            get
            {
                return _controlPort;
            }
            set
            {
                if (_controlPort != null)
                {
                    _controlPort.Dispose();
                }
                _controlPort = value;
            }
        }

        private void EnterInitState()
        {
            ControlPort = new ControlPortListener();
            HeloSender = new HeloSender(ControlPort.Port);
            HeloSender.Sent += (o, e) => Dispatcher.Invoke((Action)(() => LogBox.AppendText(e.Item + " sent" + Environment.NewLine)));
            ControlPort.LogLine += (o, e) => Dispatcher.Invoke((Action)(() => LogBox.AppendText("CTRL: " + e.Item + Environment.NewLine)));
        }
    }
}
