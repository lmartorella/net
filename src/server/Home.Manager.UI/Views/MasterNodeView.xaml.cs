using Lucky.Home.Services;
using System.Threading;
using Lucky.Home.Simulator;
using System.Windows.Controls;
using System.Threading.Tasks;

namespace Lucky.Home.Views
{
    public partial class MasterNodeView
    {
        private CancellationTokenSource _cancellationTokenSrc;

        public MasterNodeView()
        {
            InitializeComponent();

            DataContext = this;
        }

        internal void Init(MasterNode node)
        {
            _cancellationTokenSrc = new CancellationTokenSource();
            node.StartServer(_cancellationTokenSrc.Token);

            var sinkManager = Manager.GetService<MockSinkManager>();
            foreach (var sink in node.Sinks)
            {
                TabItem tabItem = new TabItem { Content = sink, Header = sinkManager.GetDisplayName(sink) };
                TabControl.Items.Add(tabItem);
            }

            node.Reset = () =>
            {
                Task.Run(() =>
                {
                    Close();
                    _cancellationTokenSrc = new CancellationTokenSource();
                    node.StartServer(_cancellationTokenSrc.Token);
                });
            };

        }

        public void Close()
        {
            _cancellationTokenSrc.Cancel();
        }
    }
}
