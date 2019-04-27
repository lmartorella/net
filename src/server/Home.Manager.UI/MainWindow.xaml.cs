using Lucky.Home.Models;
using Lucky.Home.Services;
using Lucky.Home.Simulator;
using Lucky.Home.Views;
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

            var nodes = Manager.GetService<SimulatorNodesService>().Restore();
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
            var node = Manager.GetService<SimulatorNodesService>().CreateNewMasterNode(Manager.GetService<MockSinkManager>().GetAllSinks());
            AddNewNodeTab(node);
        }

        private void AddNewNodeTab(MasterNode node)
        {
            var content = new MasterNodeView();
            content.Init(node);
            TabControl.Items.Add(new TabItem { Header = "Master Node", Content = content });
        }
    }
}
