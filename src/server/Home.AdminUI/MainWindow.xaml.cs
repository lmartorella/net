using Lucky.Home.Models;
using System.Windows;

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
    }
}
