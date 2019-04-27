using Lucky.Home.Protocol;
using Lucky.Home.Services;
using Lucky.Home.Simulator;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Lucky.Home.Views
{
    [MockSink("SYS ")]
    public partial class SystemSinkView : UserControl, ISinkMock
    {
        private bool _initialized;
        public ILogger Logger { get; set; }

        public SystemSinkView()
        {
            InitializeComponent();

            ResetReasons = Enum.GetValues(typeof(ResetReason)).Cast<ResetReason>().ToArray();
            //NodeStatus = (owner.StateProvider as IStateProviderInternal).Status;

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
    }
}
