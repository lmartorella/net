using System;
using System.Threading.Tasks;
using Lucky.Home.Serialization;
using Lucky.Services;

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
            Status = await GetBootStatus();
        }

        private async Task<NodeStatus> GetBootStatus()
        {
            NodeStatus status = new NodeStatus();
            await Read(reader =>
            {
                while (true)
                {
                    var code = reader.Read<Fourcc>().Code;
                    switch (code)
                    {
                        case "REST":
                            status.ResetReason = (ResetReason) reader.Read<ushort>();
                            break;
                        case "EXCM":
                            status.ExceptionMessage = reader.Read<DynamicString>().Str;
                            break;
                        case "EOMD":
                            return;
                        default:
                            throw new InvalidOperationException("Unknown system");
                    }
                }
            });
            Logger.Log("Getting boot status: " + status);
            return status;
        }
    }
}
