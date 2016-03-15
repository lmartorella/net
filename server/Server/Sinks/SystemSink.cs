using System;
using Lucky.Home.Serialization;
using Lucky.Services;

// ReSharper disable UnusedMember.Global

namespace Lucky.Home.Sinks
{
    [SinkId("SYS ")]
    class SystemSink : SinkBase, ISystemSink
    {
        public NodeStatus Status { get; private set; }

        public SystemSink()
        {
            Status = new NodeStatus { ResetReason = ResetReason.Waiting };
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            Status = GetBootStatus();
        }

        private NodeStatus GetBootStatus()
        {
            NodeStatus status = new NodeStatus();
            Read(reader =>
            {
                while (true)
                {
                    var code = reader.Read<Twocc>().Code;
                    switch (code)
                    {
                        case "RS":
                            status.ResetReason = (ResetReason) reader.Read<ushort>();
                            break;
                        case "EX":
                            status.ExceptionMessage = reader.Read<DynamicString>().Str;
                            break;
                        case "EN":
                            return;
                        default:
                            throw new InvalidOperationException("Unknown system");
                    }
                }
            });
            Logger.Log("Getting boot status: " + status);

            Write(writer =>
            {
                // Write none. Reset the reset reason to NONE
            });
            return status;
        }
    }
}
