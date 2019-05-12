using Lucky.Home.Models;
using Lucky.Home.Protocol;
using Lucky.Home.Services;
using Lucky.Home.Simulator;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Lucky.Home.Views
{
    [MockSink("SYS ", "System")]
    public partial class SystemSinkView : UserControl, ISinkMock
    {
        private bool _initialized;
        private ILogger Logger;
        private ISimulatedNodeInternal _node;

        public SystemSinkView()
        {
            InitializeComponent();
            DataContext = this;
        }

        public void Init(ISimulatedNode node)
        {
            _node = node as ISimulatedNodeInternal;
            Logger = Manager.GetService<ILoggerFactory>().Create("SysSink", node.Id.ToString());
            node.IdChanged += (o, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    NodeId = node.Id?.ToString();
                    Logger.SubKey = node.Id.ToString();
                });
            };

            ResetReasons = Enum.GetValues(typeof(ResetReason)).Cast<ResetReason>().ToArray();
            NodeStatus = (node as ISimulatedNodeInternal).Status;
            NodeId = node.Id?.ToString();

            _initialized = true;
        }        

        private NodeStatus NodeStatus
        {
            get
            {
                return new NodeStatus
                {
                    ExceptionMessage = ExcMsg,
                    ResetReason = ResetReason
                };
            }
            set
            {
                if (value != null)
                {
                    ExcMsg = value.ExceptionMessage;
                    ResetReason = value.ResetReason;
                }
                else
                {
                    ResetReason = ResetReason.Power;
                }
            }
        }

        public void Read(BinaryReader reader)
        {
            byte command = reader.ReadByte();
            switch (command)
            {
                case 1:
                    // Reset
                    Logger.Log("Reset!");
                    break;
                case 2:
                    // Reset EXC reason
                    ResetReason = ResetReason.None;
                    ExcMsg = null;
                    break;
            }
        }

        public void Write(BinaryWriter writer)
        {
            writer.WriteTwocc("RS");
            writer.Write((ushort)ResetReason);
            if (!string.IsNullOrEmpty(ExcMsg))
            {
                writer.WriteTwocc("EX");
                writer.WriteString(ExcMsg);
            }
            writer.WriteTwocc("EN");
        }

        private void HandleResetClick(object sender, EventArgs args)
        {
            _node.Reset();
        }

        internal static readonly DependencyProperty NodeIdProperty = DependencyProperty.Register(
           "NodeId", typeof(string), typeof(SystemSinkView), null);

        internal string NodeId
        {
            get { return (string)GetValue(NodeIdProperty); }
            set { SetValue(NodeIdProperty, value); }
        }

        internal static readonly DependencyProperty ResetReasonsProperty = DependencyProperty.Register(
           "ResetReasons", typeof(ResetReason[]), typeof(SystemSinkView), new PropertyMetadata(default(ResetReason[])));

        internal ResetReason[] ResetReasons
        {
            get { return (ResetReason[])GetValue(ResetReasonsProperty); }
            set { SetValue(ResetReasonsProperty, value); }
        }

        internal static readonly DependencyProperty ResetReasonProperty = DependencyProperty.Register(
            "ResetReason", typeof(ResetReason), typeof(SystemSinkView), new PropertyMetadata(ResetReason.Power, ResetReasonPropertyChangedCallback));

        private static void ResetReasonPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            SystemSinkView me = (SystemSinkView)dependencyObject;
            if (me._initialized)
            {
                //(_owner.StateProvider as IStateProviderInternal).Status = me.NodeStatus;
            }
        }

        internal ResetReason ResetReason
        {
            get { return (ResetReason)GetValue(ResetReasonProperty); }
            set { SetValue(ResetReasonProperty, value); }
        }

        public static readonly DependencyProperty ExcMsgProperty = DependencyProperty.Register(
            "ExcMsg", typeof(string), typeof(SystemSinkView), new PropertyMetadata(default(string), ExcMsgPropertyChangedCallback));

        public string ExcMsg
        {
            get { return (string)GetValue(ExcMsgProperty); }
            set { SetValue(ExcMsgProperty, value); }
        }

        private static void ExcMsgPropertyChangedCallback(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            SystemSinkView me = (SystemSinkView)dependencyObject;
            if (me._initialized)
            {
                //(_owner.StateProvider as IStateProviderInternal).Status = me.NodeStatus;
            }
        }

        public static readonly DependencyProperty AddSlaveCommandProperty = DependencyProperty.Register(
            "AddSlaveCommand", typeof(ICommand), typeof(SystemSinkView), null);

        public ICommand AddSlaveCommand
        {
            get { return (ICommand)GetValue(AddSlaveCommandProperty); }
            set { SetValue(AddSlaveCommandProperty, value); }
        }
    }
}
