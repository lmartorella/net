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
        }

        public static readonly DependencyProperty DevicesProperty = DependencyProperty.Register(
            "Devices", typeof (ObservableCollection<UiDevice>), typeof (DeviceView), new PropertyMetadata(default(ObservableCollection<UiDevice>)));

        public ObservableCollection<UiDevice> Devices
        {
            get { return (ObservableCollection<UiDevice>) GetValue(DevicesProperty); }
            set { SetValue(DevicesProperty, value); }
        }
    }
}
