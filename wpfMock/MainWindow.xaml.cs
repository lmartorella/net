using System;
using Lucky.Home.Core;
using Lucky.HomeMock.Core;
using Lucky.HomeMock.Sinks;

namespace Lucky.HomeMock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ILogger
    {
        private HeloSender _heloSender;
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

            var controlPort = Manager.GetService<ControlPortListener>();
            controlPort.InitSinks(new SinkBase[] { _displaySink });
            HeloSender = new HeloSender(controlPort.Port, controlPort.LocalhostMode);

            Manager.GetService<GuiLoggerFactory>().Register(this);
        }

        public void LogFormat(string type, string message, params object[] args)
        {
            LogLine(string.Format(message, args));
        }
    }
}
