using System.Windows;
using Lucky.Home.Devices;

namespace Lucky.Home
{
    public class UiDevice : DependencyObject
    {
        internal UiDevice(DeviceDescriptor desc)
        {
            DeviceType = desc.DeviceType;
            Argument = desc.Argument;
        }

        public static readonly DependencyProperty ArgumentProperty = DependencyProperty.Register(
            "Argument", typeof (string), typeof (UiDevice), new PropertyMetadata(default(string)));

        public string Argument
        {
            get { return (string) GetValue(ArgumentProperty); }
            set { SetValue(ArgumentProperty, value); }
        }

        public static readonly DependencyProperty DeviceTypeProperty = DependencyProperty.Register(
            "DeviceType", typeof (string), typeof (UiDevice), new PropertyMetadata(default(string)));

        public string DeviceType
        {
            get { return (string) GetValue(DeviceTypeProperty); }
            set { SetValue(DeviceTypeProperty, value); }
        }
    }
}
