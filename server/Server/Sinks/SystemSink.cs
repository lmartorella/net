using System;
using Lucky.Home.Serialization;
using Lucky.Services;
using System.Threading.Tasks;

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

        protected override async void OnInitialize()
        {
            base.OnInitialize();
            await FetchStatus();
        }

        private async Task FetchStatus()
        { 
            Status = GetBootStatus();
            if (Status == null)
            {
                // Schedule a retry
                await Task.Delay(1500);
                await FetchStatus();
            }
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
                        case null:
                            status = null;
                            return;
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
            if (status == null)
            {
                return null;
            }

            Logger.Log("Getting boot status: " + status);

            Write(writer =>
            {
                // Write none. Reset the reset reason to NONE
            });
            return status;
        }
    }
}
