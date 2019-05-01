using Lucky.Home.Services;
using Lucky.Home.Simulator;
using System.IO;
using System.Text;
using System.Windows.Controls;

namespace Lucky.Home.Views
{
    [MockSink("LINE")]
    public partial class DisplaySinkView : UserControl, ISinkMock
    {
        private ILogger Logger;

        public DisplaySinkView()
        {
            InitializeComponent();
        }

        public void Init(ISimulatedNode node)
        {
            Logger = Manager.GetService<ILoggerFactory>().Create("DisplaySink", node.Id.ToString());
            node.IdChanged += (o, e) => Logger.SubKey = node.Id.ToString();
        }

        public void Read(BinaryReader reader)
        {
            var l = reader.ReadInt16();
            string str = Encoding.ASCII.GetString(reader.ReadBytes(l));
            Dispatcher.Invoke(() =>
            {
                DisplayBox.Text = str;
            });
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((short)1);
            writer.Write((short)20);
        }
    }
}
