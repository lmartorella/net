using System;
using System.Threading.Tasks;
using Lucky.Home.Core;

namespace Lucky.Home.Sinks
{
    [SinkId("SYS ")]
    class SystemSink : Sink, ISystemSink
    {
        public NodeStatus Status { get; private set; }

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
            return status;
        }
    }
}
