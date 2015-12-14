using System.Windows;

namespace Lucky.Home
{
    /// <summary>
    /// Interaction logic for CreateDeviceWindow.xaml
    /// </summary>
    public partial class CreateDeviceWindow
    {
        public CreateDeviceWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public static readonly DependencyProperty SinksProperty = DependencyProperty.Register(
            "Sinks", typeof (string[]), typeof (CreateDeviceWindow), new PropertyMetadata(default(string[])));

        public string[] Sinks
        {
            get { return (string[]) GetValue(SinksProperty); }
            set { SetValue(SinksProperty, value); }
        }

        public static readonly DependencyProperty DeviceTypesProperty = DependencyProperty.Register(
            "DeviceTypes", typeof (string[]), typeof (CreateDeviceWindow), new PropertyMetadata(default(string[])));

        public string[] DeviceTypes
        {
            get { return (string[]) GetValue(DeviceTypesProperty); }
            set { SetValue(DeviceTypesProperty, value); }
        }

        public static readonly DependencyProperty DeviceTypeProperty = DependencyProperty.Register(
            "DeviceType", typeof (string), typeof (CreateDeviceWindow), new PropertyMetadata(default(string)));

        public string DeviceType
        {
            get { return (string) GetValue(DeviceTypeProperty); }
            set { SetValue(DeviceTypeProperty, value); }
        }

        public static readonly DependencyProperty SinkIdProperty = DependencyProperty.Register(
            "SinkId", typeof (string), typeof (CreateDeviceWindow), new PropertyMetadata(default(string)));

        public string SinkId
        {
            get { return (string) GetValue(SinkIdProperty); }
            set { SetValue(SinkIdProperty, value); }
        }

        public static readonly DependencyProperty ArgumentProperty = DependencyProperty.Register(
            "Argument", typeof (string), typeof (CreateDeviceWindow), new PropertyMetadata(default(string)));

        public string Argument
        {
            get { return (string) GetValue(ArgumentProperty); }
            set { SetValue(ArgumentProperty, value); }
        }

        private void OkClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
