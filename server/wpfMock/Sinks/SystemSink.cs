using System;
using System.IO;
using Lucky.Home;
using Lucky.Services;

namespace Lucky.HomeMock.Sinks
{
    internal class SystemSink : SinkBase
    {
        private ResetReason _resetReason;
        private string _excMsg;
        private readonly bool _initialized;

        public SystemSink()
            : base("SYS ")
        {
            NodeStatus = Manager.GetService<SinkStateManager>().NodeStatus;
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

        public ResetReason ResetReason
        {
            get
            {
                return _resetReason;
            }
            set
            {
                _resetReason = value;
                if (_initialized)
                {
                    Manager.GetService<SinkStateManager>().NodeStatus = NodeStatus;
                }
            }
        }

        public string ExcMsg
        {
            get
            {
                return _excMsg;
            }
            set
            {
                _excMsg = value;
                if (_initialized)
                {
                    Manager.GetService<SinkStateManager>().NodeStatus = NodeStatus;
                }
            }
        }

        public override void Read(BinaryReader reader)
        {
            throw new NotImplementedException();
        }

        public override void Write(BinaryWriter writer)
        {
            WriteFourcc(writer, "REST");
            writer.Write((ushort)ResetReason);
            if (!string.IsNullOrEmpty(ExcMsg))
            {
                WriteFourcc(writer, "EXCM");
                WriteString(writer, ExcMsg);
            }
            WriteFourcc(writer, "EOMD");
        }
    }
}