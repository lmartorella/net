using System.Windows;

namespace Lucky.Home.Models
{
    public class Switch : DependencyObject
    {
        public string Name { get; private set; }

        public Switch(bool value, string name)
        {
            Value = value;
            Name = name;
        }

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof (bool), typeof (Switch), new PropertyMetadata(default(bool)));

        public bool Value
        {
            get { return (bool) GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
    }
}