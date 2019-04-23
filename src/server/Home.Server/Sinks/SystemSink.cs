using System;
using Lucky.Serialization;
using Lucky.Services;
using System.Threading.Tasks;

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

        protected override async Task OnInitialize()
        {
            await base.OnInitialize();
            await FetchStatus();
        }

        public async Task FetchStatus()
        { 
            Status = await GetBootStatus();
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

        private const string ResetCode = "RS";
        private const string ExceptionText = "EX";
        private const string EndOfMetadataText = "EN";
        private const string BusMasterStatitstics = "BM";


        private class BusMasterStats
        {
#pragma warning disable CS0649 
            public byte SocketTimeoutCount;
#pragma warning restore CS0649 
        }

        private async Task<NodeStatus> GetBootStatus()
        {
            NodeStatus status = new NodeStatus();
            BusMasterStats stats = null;
            await Read(async reader =>
            {
                while (true)
                {
                    var code = (await reader.Read<Twocc>())?.Code;
                    switch (code)
                    {
                        case null: // EOF
                            status = null;
                            return;
                        case ResetCode:
                            status.ResetReason = (ResetReason) await reader.Read<ushort>();
                            break;
                        case ExceptionText:
                            status.ExceptionMessage = (await reader.Read<DynamicString>()).Str;
                            break;
                        case BusMasterStatitstics:
                            stats = await reader.Read<BusMasterStats>();
                            break;
                        case EndOfMetadataText:
                            return;
                        default:
                            throw new InvalidOperationException("Unknown system");
                    }
                }
            });
            if (status == null)
            {
                // EOF?
                return null;
            }

            if (status.ResetReason != ResetReason.None)
            {
                Logger.Log("Getting boot status: " + status);
            }
            if (stats?.SocketTimeoutCount > 0)
            {
                Logger.Warning("Master Stats", "SocketTimeouts#", stats.SocketTimeoutCount);
            }

            if (status.ResetReason != ResetReason.None)
            {
                await Write(async writer =>
                {
                    await writer.Write(SysCommand.ClearResetReason);
                });
            }
            return status;
        }

        /// <summary>
        /// Reset the device
        /// </summary>
        public Task Reset()
        {
            return Write(async writer =>
            {
                await writer.Write(SysCommand.Reset);
            });
        }
    }
}
