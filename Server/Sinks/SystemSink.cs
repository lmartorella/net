using System;
using System.Threading.Tasks;
using Lucky.Home.Core;

namespace Lucky.Home.Sinks
{
    [SinkId("SYS ")]
    class SystemSink : Sink, ISystemSink
    {
        async public Task<NodeStatus> GetBootStatus()
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
                            status.ResetReason = (ResetReason)reader.Read<ushort>();
                            break;
                        case "EXCM":
                            status.ExceptionMessage = reader.Read<string>();
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
