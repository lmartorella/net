using System.Windows;

namespace Lucky.Home
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register(
            "Connection", typeof (Connection), typeof (MainWindow), new PropertyMetadata(default(Connection)));

        public Connection Connection
        {
            get { return (Connection) GetValue(ConnectionProperty); }
            set { SetValue(ConnectionProperty, value); }
        }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            var connection = new TcpConnection();
            connection.Connect();
            Connection = connection;

            //Connection = new Design.SampleData1();
        }
    }
}
