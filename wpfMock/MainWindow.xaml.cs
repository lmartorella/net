using System;
using System.Net;
using System.Windows;
using System.Windows.Threading;
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
        private DisplaySink _displaySink;

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

        private void LogLine(string line)
        {
            Dispatcher.Invoke(() => LogBox.AppendText(line + Environment.NewLine));
        }

        private void EnterInitState()
        {
            _displaySink = new DisplaySink();
            _displaySink.Data += (sender, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    DisplayBox.Text = args.Item;
                });
            };

            ControlPort = new ControlPortListener(new SinkBase[] { _displaySink} );
            HeloSender = new HeloSender(ControlPort.Port);
            HeloSender.Sent += (o, e) => LogLine(e.Item + " sent");
            ControlPort.LogLine += (o, e) => LogLine("CTRL: " + e.Item);

            LogLine("Started instance " + App.Current.InstanceIndex);
        }
    }
}
