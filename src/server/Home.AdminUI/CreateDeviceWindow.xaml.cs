using System.Windows;
using Lucky.Home.Devices;

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

        internal static readonly DependencyProperty DeviceTypesProperty = DependencyProperty.Register(
            "DeviceTypes", typeof(DeviceTypeDescriptor[]), typeof(CreateDeviceWindow), new PropertyMetadata(default(DeviceTypeDescriptor[])));

        internal DeviceTypeDescriptor[] DeviceTypes
        {
            get { return (DeviceTypeDescriptor[])GetValue(DeviceTypesProperty); }
            set { SetValue(DeviceTypesProperty, value); }
        }

        internal static readonly DependencyProperty DeviceTypeProperty = DependencyProperty.Register(
            "DeviceType", typeof(DeviceTypeDescriptor), typeof(CreateDeviceWindow), new PropertyMetadata(default(DeviceTypeDescriptor)));

        internal DeviceTypeDescriptor DeviceType
        {
            get { return (DeviceTypeDescriptor)GetValue(DeviceTypeProperty); }
            set { SetValue(DeviceTypeProperty, value); }
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
