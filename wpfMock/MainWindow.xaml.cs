using System;
using System.Windows;
using System.Windows;
using Lucky.HomeMock.Core;

namespace wpfMock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HeloSender _heloSender;
        private HomeReceiver _homeReceiver;

        public MainWindow()
        {
            InitializeComponent();

            _heloSender = new HeloSender();
            _heloSender.Sent += (o,e) => Dispatcher.Invoke((Action)(() => LogBox.AppendText("Helo sent\n")));
            _homeReceiver = new HomeReceiver();
            _homeReceiver.HomeFound += (o, e) => Dispatcher.Invoke((Action)(() => LogBox.AppendText("Found home: " + _homeReceiver.HomeHost + ":" + _homeReceiver.HomePort + "\n")));
        }
    }
}
