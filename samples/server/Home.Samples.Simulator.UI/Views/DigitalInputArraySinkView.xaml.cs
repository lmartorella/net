using Lucky.Home.Services;
using Lucky.Home.Simulator;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Lucky.Home.Views
{
    [MockSink("DIAR")]
    public partial class DigitalInputArraySinkView : UserControl, ISinkMock
    {
        private ILogger Logger;

        public DigitalInputArraySinkView()
        {
            InitializeComponent();

            SwitchesCount = 8;
        }

        public void Init(ISimulatedNode node)
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("DiarSink", node.Id.ToString());
            node.IdChanged += (o, e) => Logger.SubKey = node.Id.ToString();
        }

        public void Read(BinaryReader reader)
        {
            throw new System.NotImplementedException();
        }

        public void Write(BinaryWriter writer)
        {
            // Write bare switch cound
            Dispatcher.Invoke(() =>
            {
                var count = SwitchesCount;
                writer.Write((byte)count);
                int bytes = ((count - 1) / 8) + 1;

                byte[] ret = new byte[bytes];
                for (int i = 0; i < count; i++)
                {
                    // Pack bits
                    ret[i / 8] = (byte)(ret[i / 8] | (Inputs[i].Value ? (1 << (i % 8)) : 0));
                }
                writer.Write(ret);
            });
        }

        public static readonly DependencyProperty SwitchesCountProperty = DependencyProperty.Register(
            "SwitchesCount", typeof(int), typeof(DigitalInputArraySinkView), new PropertyMetadata(default(int), HandleSwitchesCountChanged));

        public int SwitchesCount
        {
            get { return (int)GetValue(SwitchesCountProperty); }
            set { SetValue(SwitchesCountProperty, value); }
        }

        private static void HandleSwitchesCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DigitalInputArraySinkView me = (DigitalInputArraySinkView)d;
            me.Inputs = new ObservableCollection<Switch>(Enumerable.Range(0, me.SwitchesCount).Select(i => new Switch(false, "Input " + i)));
        }

        public static readonly DependencyProperty InputsProperty = DependencyProperty.Register(
            "Inputs", typeof(ObservableCollection<Switch>), typeof(DigitalInputArraySinkView), new PropertyMetadata(default(ObservableCollection<Switch>)));

        public ObservableCollection<Switch> Inputs
        {
            get { return (ObservableCollection<Switch>)GetValue(InputsProperty); }
            set { SetValue(InputsProperty, value); }
        }
    }
}
