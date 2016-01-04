using System.Linq;
using System.Windows;
using Lucky.Home.Devices;

namespace Lucky.Home
{
    public class UiDevice : DependencyObject
    {
        internal UiDevice(DeviceDescriptor desc)
        {
            DeviceType = desc.DeviceType;
            Arguments = desc.Arguments;
            SinksDescription = string.Format("Sinks: {0}", string.Join(", ", desc.SinkPaths.Select(s => s.ToString())));
        }

        public static readonly DependencyProperty ArgumentsProperty = DependencyProperty.Register(
            "Arguments", typeof (object[]), typeof (UiDevice), new PropertyMetadata(default(object[])));

        public object[] Arguments
        {
            get { return (object[])GetValue(ArgumentsProperty); }
            set { SetValue(ArgumentsProperty, value); }
        }

        public static readonly DependencyProperty DeviceTypeProperty = DependencyProperty.Register(
            "DeviceType", typeof (string), typeof (UiDevice), new PropertyMetadata(default(string)));

        public string DeviceType
        {
            get { return (string) GetValue(DeviceTypeProperty); }
            set { SetValue(DeviceTypeProperty, value); }
        }

        public static readonly DependencyProperty SinksDescriptionProperty = DependencyProperty.Register(
            "SinksDescription", typeof (string), typeof (UiDevice), new PropertyMetadata(default(string)));

        public string SinksDescription
        {
            get { return (string) GetValue(SinksDescriptionProperty); }
            set { SetValue(SinksDescriptionProperty, value); }
        }
    }
}
