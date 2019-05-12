using Lucky.Home.Models;
using Lucky.Home.Services;
using Lucky.Home.Simulator;
using Lucky.Home.Views;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Lucky.Home
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            RefreshClicked(null, null);

            ClearLogCommand = new UiCommand(() =>
            {
                LogBox.Clear();
            });

            Manager.GetService<GuiLoggerFactory>().Register(this);

            var nodes = Manager.GetService<SimulatorNodesService>().Restore(Dispatcher);
            foreach (var node in nodes)
            {
                AddNewNodeTab(node);
            }
        }

        internal static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register(
            "Connection", typeof (Connection), typeof (MainWindow), new PropertyMetadata(default(Connection)));

        internal Connection Connection
        {
            get { return (Connection) GetValue(ConnectionProperty); }
            set { SetValue(ConnectionProperty, value); }
        }

        public static readonly DependencyProperty IsRefreshEnabledProperty = DependencyProperty.Register(
            "IsRefreshEnabled", typeof (bool), typeof (MainWindow), new PropertyMetadata(default(bool)));

        public bool IsRefreshEnabled
        {
            get { return (bool) GetValue(IsRefreshEnabledProperty); }
            set { SetValue(IsRefreshEnabledProperty, value); }
        }

        private void RefreshClicked(object sender, RoutedEventArgs routedEventArgs)
        {
            IsRefreshEnabled = false;
            if (Connection != null)
            {
                Connection.Dispose();
            }
            Connection = new Connection(() => IsRefreshEnabled = true);
        }

        private void NewNodeClicked(object sender, RoutedEventArgs e)
        {
            var node = Manager.GetService<SimulatorNodesService>().CreateNewMasterNode(Dispatcher, Manager.GetService<MockSinkManager>().GetAllSinks());
            AddNewNodeTab(node);
        }

        private void AddNewNodeTab(MasterNode node)
        {
            var content = new NodeView();
            content.Init(node);
            TabControl.Items.Add(new TabItem { Header = "Master Node", Content = content });
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

        public static readonly DependencyProperty ClearLogCommandProperty = DependencyProperty.Register(
            "ClearLogCommand", typeof(UiCommand), typeof(NodeView), new PropertyMetadata(default(UiCommand)));

        public UiCommand ClearLogCommand
        {
            get { return (UiCommand)GetValue(ClearLogCommandProperty); }
            set { SetValue(ClearLogCommandProperty, value); }
        }

        public static readonly DependencyProperty VerboseLogProperty = DependencyProperty.Register(
            "VerboseLog", typeof(bool), typeof(NodeView), new PropertyMetadata(false));

        public bool VerboseLog
        {
            get { return (bool)GetValue(VerboseLogProperty); }
            set { SetValue(VerboseLogProperty, value); }
        }
    }
}
