using System.Windows;

namespace Lucky.Home
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public Connection Connection;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            Connection = new Connection();
        }
    }
}
