using Lucky.Home.Models;
using Lucky.Home.Services;
using Lucky.Home.Simulator;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Lucky.Home.Views
{
    /// <summary>
    /// Mock sink + UI for bit output array
    /// </summary>
    [MockSink("DOAR", "Output array")]
    public partial class DigitalOutputArraySinkView : UserControl, ISinkMock
    {
        private ILogger Logger;

        public DigitalOutputArraySinkView()
        {
            InitializeComponent();

            SwitchesCount = 8;
            DataContext = this;
        }

        public void Init(ISimulatedNode node)
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("DoarSink", node.Id.ToString());
            node.IdChanged += (o, e) => Logger.SubKey = node.Id.ToString();
        }

        public void Read(BinaryReader reader)
        {
            int switchesCount = reader.ReadByte();
            var byteCount = (switchesCount - 1) / 8 + 1;
            byte[] data = reader.ReadBytes(byteCount);

            // Write bare switch cound
            Dispatcher.Invoke(() =>
            {
                for (int i = 0; i < switchesCount; i++)
                {
                    // Pack bits
                    Outputs[i].Value = (data[i / 8] & (1 << (i % 8))) != 0;
                }
            });
        }

        public void Write(BinaryWriter writer)
        {
            // Write bare switch cound
            Dispatcher.Invoke(() =>
            {
                var count = SwitchesCount;
                writer.Write((ushort)count);
            });
        }

        public static readonly DependencyProperty SwitchesCountProperty = DependencyProperty.Register(
           "SwitchesCount", typeof(int), typeof(DigitalOutputArraySinkView), new PropertyMetadata(default(int), HandleSwitchesCountChanged));

        public int SwitchesCount
        {
            get { return (int)GetValue(SwitchesCountProperty); }
            set { SetValue(SwitchesCountProperty, value); }
        }

        private static void HandleSwitchesCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DigitalOutputArraySinkView me = (DigitalOutputArraySinkView)d;
            me.Outputs = new ObservableCollection<Switch>(Enumerable.Range(0, me.SwitchesCount).Select(i => new Switch(false, "Output " + i)));
        }

        public static readonly DependencyProperty OutputsProperty = DependencyProperty.Register(
            "Outputs", typeof(ObservableCollection<Switch>), typeof(DigitalOutputArraySinkView), new PropertyMetadata(default(ObservableCollection<Switch>)));

        public ObservableCollection<Switch> Outputs
        {
            get { return (ObservableCollection<Switch>)GetValue(OutputsProperty); }
            set { SetValue(OutputsProperty, value); }
        }
    }
}
