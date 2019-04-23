using System.Collections.ObjectModel;
using System.Windows;

namespace Lucky.Home
{
    /// <summary>
    /// Interaction logic for DeviceView.xaml
    /// </summary>
    public partial class DeviceView
    {
        public DeviceView()
        {
            InitializeComponent();
            List.DataContext = this;

            DeleteCommand = new UiCommand(() =>
            {
                TcpConnection.DeleteDevice(SelectedDevice);
            }, () => SelectedDevice != null);
        }

        public UiDevice SelectedDevice
        {
            get
            {
                return List.SelectedItem as UiDevice;
            }
        }

        public static readonly DependencyProperty DevicesProperty = DependencyProperty.Register(
            "Devices", typeof (ObservableCollection<UiDevice>), typeof (DeviceView), new PropertyMetadata(default(ObservableCollection<UiDevice>)));

        public ObservableCollection<UiDevice> Devices
        {
            get { return (ObservableCollection<UiDevice>) GetValue(DevicesProperty); }
            set { SetValue(DevicesProperty, value); }
        }

        public static readonly DependencyProperty DeleteCommandProperty = DependencyProperty.Register(
            "DeleteCommand", typeof (UiCommand), typeof (DeviceView), new PropertyMetadata(default(UiCommand)));

        public UiCommand DeleteCommand
        {
            get { return (UiCommand) GetValue(DeleteCommandProperty); }
            set { SetValue(DeleteCommandProperty, value); }
        }

        public static readonly DependencyProperty TcpConnectionProperty = DependencyProperty.Register(
    "TcpConnection", typeof(Connection), typeof(DeviceView), new PropertyMetadata(null, HandleConnectionChanged));

        private static void HandleConnectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                ((Connection)e.NewValue).NodeSelectionChanged += (o, e1) =>
                {
                    ((DeviceView)d).UpdateMenuItems();
                };
            }
        }

        public Connection TcpConnection
        {
            get { return (Connection)GetValue(TcpConnectionProperty); }
            set { SetValue(TcpConnectionProperty, value); }
        }

        private void UpdateMenuItems()
        {
            DeleteCommand.RaiseCanExecuteChanged();
        }
    }
}
