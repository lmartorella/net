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

            ClearLogCommand = new UiCommand(() =>
            {
                LogBox.Clear();
            });
        }

        public void Init(MasterNode node)
        {
            node.StartServer(_cancellationTokenSrc.Token);
        }

        public void Close()
        {
            _cancellationTokenSrc.Cancel();
        }

        private void LogLine(string line, bool verbose)
        {
            Dispatcher.Invoke(() =>
            {
                if (!verbose || VerboseLog)
                {
                    LogBox.AppendText(line + Environment.NewLine);
                }
            });
        }

        public void LogFormat(bool verbose, string type, string message, params object[] args)
        {
            LogLine(string.Format(message, args), verbose);
        }

        public static readonly DependencyProperty ClearLogCommandProperty = DependencyProperty.Register(
            "ClearLogCommand", typeof(UiCommand), typeof(MasterNodeView), new PropertyMetadata(default(UiCommand)));

        public UiCommand ClearLogCommand
        {
            get { return (UiCommand)GetValue(ClearLogCommandProperty); }
            set { SetValue(ClearLogCommandProperty, value); }
        }

        public static readonly DependencyProperty VerboseLogProperty = DependencyProperty.Register(
            "VerboseLog", typeof(bool), typeof(MasterNodeView), new PropertyMetadata(false));

        public bool VerboseLog
        {
            get { return (bool)GetValue(VerboseLogProperty); }
            set { SetValue(VerboseLogProperty, value); }
        }
    }
}
