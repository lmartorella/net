using System;
using System.Windows;

namespace Lucky.Home.Models
{
    /// <summary>
    /// The model behind a single UI switch
    /// </summary>
    public class Switch : DependencyObject
    {
        public string Name { get; private set; }

        public Switch(bool value, string name)
        {
            Value = value;
            Name = name;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == ValueProperty)
            {
                ValueChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler ValueChanged;

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof (bool), typeof (Switch), new PropertyMetadata(default(bool)));

        public bool Value
        {
            get { return (bool) GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
    }
}