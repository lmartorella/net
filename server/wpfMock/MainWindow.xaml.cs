using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Lucky.Home;
using Lucky.HomeMock.Core;
using Lucky.HomeMock.Sinks;
using Lucky.Services;

namespace Lucky.HomeMock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : ILogger
    {
        private HeloSender _heloSender;
        private DisplaySink _displaySink;
        private readonly SystemSink _systemSink;
        private readonly DigitalInputArraySink _digitalInputsSink;
        private readonly DigitalOutputArraySink _digitalOutputsSink;

        public MainWindow()
        {
            InitializeComponent();

            _displaySink = new DisplaySink();
            _systemSink = new SystemSink();

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

            SwitchesCount = 8;
            _digitalInputsSink = new DigitalInputArraySink(this);
            _digitalOutputsSink = new DigitalOutputArraySink(this);

            var controlPort = Manager.GetService<ControlPortListener>();
            controlPort.StartServer();
            controlPort.InitSinks(new SinkMockBase[] { _displaySink, _systemSink, _digitalInputsSink, _digitalOutputsSink });
            HeloSender = new HeloSender(controlPort.Port, controlPort.LocalhostMode);

            Manager.GetService<GuiLoggerFactory>().Register(this);
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

        public void LogFormat(string type, string message, params object[] args)
        {
            LogLine(string.Format(message, args));
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
            me._systemSink.ResetReason = me.ResetReason;
            me._systemSink.ExcMsg = me.ExcMsg;
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
    }
}
