using System;
using System.Collections.ObjectModel;
using System.Windows;

namespace Lucky.Home
{
    public abstract class Connection : DependencyObject, IDisposable
    {
        protected Connection()
        {
            Nodes = new ObservableCollection<UiNode>();
            Devices = new ObservableCollection<UiDevice>();
        }

        public static readonly DependencyProperty StatusTextProperty = DependencyProperty.Register(
            "StatusText", typeof (string), typeof (Connection), new PropertyMetadata(default(string)));

        public string StatusText
        {
            get { return (string) GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }

        public static readonly DependencyProperty NodesProperty = DependencyProperty.Register(
            "Nodes", typeof (ObservableCollection<UiNode>), typeof (Connection), new PropertyMetadata(default(ObservableCollection<UiNode>)));

        public ObservableCollection<UiNode> Nodes
        {
            get { return (ObservableCollection<UiNode>) GetValue(NodesProperty); }
            set { SetValue(NodesProperty, value); }
        }

        public static readonly DependencyProperty DevicesProperty = DependencyProperty.Register(
            "Devices", typeof (ObservableCollection<UiDevice>), typeof (Connection), new PropertyMetadata(default(ObservableCollection<UiDevice>)));

        public ObservableCollection<UiDevice> Devices
        {
            get { return (ObservableCollection<UiDevice>) GetValue(DevicesProperty); }
            set { SetValue(DevicesProperty, value); }
        }

        public virtual void Dispose()
        {
        }
    }
}
