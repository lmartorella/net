using System;
using System.Linq;
using System.Windows;
using Lucky.Home.Services;
using System.Threading;
using Lucky.Home.Models;
using Lucky.Home.Simulator;

namespace Lucky.Home.Views
{
    public partial class MasterNodeView
    {
        private CancellationTokenSource _cancellationTokenSrc = new CancellationTokenSource();

        public MasterNodeView()
        {
            InitializeComponent();

            DataContext = this;
        }

        internal void Init(MasterNode node)
        {
            node.StartServer(_cancellationTokenSrc.Token);
        }

        public void Close()
        {
            _cancellationTokenSrc.Cancel();
        }
    }
}
