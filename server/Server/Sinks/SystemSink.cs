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

        private enum SysCommand : byte
        {
            Reset = 1,
            ClearResetReason
        }

        private class BusMasterStats
        {
            public byte SocketTimeoutCount;
        }

        private NodeStatus GetBootStatus()
        {
            NodeStatus status = new NodeStatus();
            Read(reader =>
            {
                while (true)
                {
                    var code = reader.Read<Twocc>()?.Code;
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
                        case "BM":
                            var s = reader.Read<BusMasterStats>();
                            if (s.SocketTimeoutCount > 0)
                            {
                                Logger.Warning("Master Stats", "SocketTimeouts#", s.SocketTimeoutCount);
                            }
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
                writer.Write(SysCommand.ClearResetReason);
            });
            return status;
        }

        public async Task Reset()
        {
            Write(writer =>
            {
                writer.Write(SysCommand.Reset);
            });
        }
    }
}
