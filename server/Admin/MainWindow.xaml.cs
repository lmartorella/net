﻿using System.Windows;

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

            Connection = new TcpConnection();
            //Connection = new SampleData1();
        }

        public static readonly DependencyProperty ConnectionProperty = DependencyProperty.Register(
            "Connection", typeof (Connection), typeof (MainWindow), new PropertyMetadata(default(Connection)));

        public Connection Connection
        {
            get { return (Connection) GetValue(ConnectionProperty); }
            set { SetValue(ConnectionProperty, value); }
        }

        private void RefreshClicked(object sender, RoutedEventArgs routedEventArgs)
        {
            Connection.Dispose();
            Connection = new TcpConnection();
        }
    }
}
