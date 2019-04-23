using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Lucky.Home;
using Lucky.HomeMock.Core;
using Lucky.HomeMock.Sinks;
using Lucky.Home.Services;
using System.Threading;

namespace Lucky.HomeMock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private HeloSender _heloSender;
        private DisplaySink _displaySink;
        private readonly SystemSink _systemSink;
        private readonly GardenSink _gardenSink;
        private readonly DigitalInputArraySink _digitalInputsSink;
        private readonly DigitalOutputArraySink _digitalOutputsSink;
        private readonly FlowSinkMock _flowSink;
        private readonly TemperatureSink _temperatureSink;
        private SystemSink _childSystemSink;
        private SamilPanelMock _solarSink;
        private CommandMockSink _commandSink;
        private CancellationTokenSource _cancellationTokenSrc = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();

            _displaySink = new DisplaySink();
            _systemSink = new SystemSink(false);
            _gardenSink = new GardenSink();
            _temperatureSink = new TemperatureSink();

            ResetReasons = Enum.GetValues(typeof(ResetReason)).Cast<ResetReason>().ToArray();
            ResetReason = _systemSink.ResetReason;
            ExcMsg = _systemSink.ExcMsg;
            DataContext = this;

            _displaySink.Data += (sender, args) =>
            {
                Dispatcher.Invoke(() =>
                {
                    DisplayBox.Text = args.Item;
                });
            };
            ClearLogCommand = new UiCommand(() =>
            {
                LogBox.Clear();
            });

            SwitchesCount = 8;
            _digitalInputsSink = new DigitalInputArraySink(this);
            _digitalOutputsSink = new DigitalOutputArraySink(this);
            _flowSink = new FlowSinkMock();

            _solarSink = new SamilPanelMock();
            _commandSink = new CommandMockSink(this);
            _solarSink.LogLine += (sender, args) =>
            {
                LogLine(args.Item, false);
            };

            var controlPort = Manager.GetService<ControlPortListener>();
            controlPort.StartServer(_cancellationTokenSrc.Token);
            _childSystemSink = new SystemSink(true);
            HeloSender = new HeloSender(controlPort.Port, controlPort.LocalhostMode);

            var sinks = new SinkMockBase[] { _displaySink, _systemSink, _digitalInputsSink, _digitalOutputsSink, _solarSink, _commandSink, _gardenSink, _flowSink, _temperatureSink };
            controlPort.InitSinks(sinks, new[] { _childSystemSink }, HeloSender);

            foreach (var sink in sinks)
            {
                sink.LogLine += (sender, args) =>
                {
                    LogLine(args.Item, false);
                };
            }

            Manager.GetService<GuiLoggerFactory>().Register(this);

            Closed += (o, e) =>
            {
                _cancellationTokenSrc.Cancel();
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

        private void LogLine(string line, bool verbose)
        {
            Dispatcher.Invoke(() =>
            {
                if (!verbose || VerboseLog)
                {
                    LogBox.AppendText(line + Environment.NewLine);
                }
            });
        }

        public void LogFormat(bool verbose, string type, string message, params object[] args)
        {
            LogLine(string.Format(message, args), verbose);
        }

        public static readonly DependencyProperty ResetReasonsProperty = DependencyProperty.Register(
            "ResetReasons", typeof (ResetReason[]), typeof (MainWindow), new PropertyMetadata(default(ResetReason[])));

        public ResetReason[] ResetReasons
        {
            get { return (ResetReason[]) GetValue(ResetReasonsProperty); }
            set { SetValue(ResetReasonsProperty, value); }
        }

        public static readonly DependencyProperty ResetReasonProperty = DependencyProperty.Register(
            "ResetReason", typeof (ResetReason), typeof (MainWindow), new PropertyMetadata(ResetReason.Power, ResetReasonPropertyChangedCallback));

        private static void ResetReasonPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            MainWindow me = (MainWindow)dependencyObject;
            var sink = me.BindSlave ? me._childSystemSink : me._systemSink;
            sink.ResetReason = me.ResetReason;
            sink.ExcMsg = me.ExcMsg;
            if (sink.ResetReason != ResetReason.None && me.BindSlave)
            {
                me._heloSender.ChildChanged = true;
            }
        }

        public ResetReason ResetReason
        {
            get { return (ResetReason) GetValue(ResetReasonProperty); }
            set { SetValue(ResetReasonProperty, value); }
        }

        public static readonly DependencyProperty ExcMsgProperty = DependencyProperty.Register(
            "ExcMsg", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string), ResetReasonPropertyChangedCallback));

        public string ExcMsg
        {
            get { return (string) GetValue(ExcMsgProperty); }
            set { SetValue(ExcMsgProperty, value); }
        }

        public static readonly DependencyProperty SwitchesCountProperty = DependencyProperty.Register(
            "SwitchesCount", typeof (int), typeof (MainWindow), new PropertyMetadata(default(int), HandleSwitchesCountChanged));

        public int SwitchesCount
        {
            get { return (int) GetValue(SwitchesCountProperty); }
            set { SetValue(SwitchesCountProperty, value); }
        }

        private static void HandleSwitchesCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MainWindow me = (MainWindow) d;
            me.Inputs = new ObservableCollection<Switch>(Enumerable.Range(0, me.SwitchesCount).Select(i => new Switch(false, "Input " + i)));
            me.Outputs = new ObservableCollection<Switch>(Enumerable.Range(0, me.SwitchesCount).Select(i => new Switch(false, "Output " + i)));
        }

        public static readonly DependencyProperty InputsProperty = DependencyProperty.Register(
            "Inputs", typeof(ObservableCollection<Switch>), typeof(MainWindow), new PropertyMetadata(default(ObservableCollection<Switch>)));

        public ObservableCollection<Switch> Inputs
        {
            get { return (ObservableCollection<Switch>)GetValue(InputsProperty); }
            set { SetValue(InputsProperty, value); }
        }

        public static readonly DependencyProperty OutputsProperty = DependencyProperty.Register(
            "Outputs", typeof (ObservableCollection<Switch>), typeof (MainWindow), new PropertyMetadata(default(ObservableCollection<Switch>)));

        public ObservableCollection<Switch> Outputs
        {
            get { return (ObservableCollection<Switch>) GetValue(OutputsProperty); }
            set { SetValue(OutputsProperty, value); }
        }

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            "Command", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string Command
        {
            get { return (string)GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly DependencyProperty CommandLogProperty = DependencyProperty.Register(
            "CommandLog", typeof(string), typeof(MainWindow), new PropertyMetadata(default(string)));

        public string CommandLog
        {
            get { return (string)GetValue(CommandLogProperty); }
            set { SetValue(CommandLogProperty, value); }
        }

        public static readonly DependencyProperty SendCommandCommandProperty = DependencyProperty.Register(
            "SendCommandCommand", typeof(UiCommand), typeof(MainWindow), new PropertyMetadata(default(UiCommand)));

        public UiCommand SendCommandCommand
        {
            get { return (UiCommand)GetValue(SendCommandCommandProperty); }
            set { SetValue(SendCommandCommandProperty, value); }
        }

        public static readonly DependencyProperty ClearLogCommandProperty = DependencyProperty.Register(
            "ClearLogCommand", typeof(UiCommand), typeof(MainWindow), new PropertyMetadata(default(UiCommand)));

        public UiCommand ClearLogCommand
        {
            get { return (UiCommand)GetValue(ClearLogCommandProperty); }
            set { SetValue(ClearLogCommandProperty, value); }
        }

        public static readonly DependencyProperty BindSlaveProperty = DependencyProperty.Register(
            "BindSlave", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool BindSlave
        {
            get { return (bool)GetValue(BindSlaveProperty); }
            set { SetValue(BindSlaveProperty, value); }
        }

        public static readonly DependencyProperty VerboseLogProperty = DependencyProperty.Register(
            "VerboseLog", typeof(bool), typeof(MainWindow), new PropertyMetadata(false));

        public bool VerboseLog
        {
            get { return (bool)GetValue(VerboseLogProperty); }
            set { SetValue(VerboseLogProperty, value); }
        }
    }
}
