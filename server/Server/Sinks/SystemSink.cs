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

        public async Task FetchStatus()
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

        private const string ResetCode = "RS";
        private const string ExceptionText = "EX";
        private const string EndOfMetadataText = "EN";
        private const string BusMasterStatitstics = "BM";


        private class BusMasterStats
        {
            public byte SocketTimeoutCount;
        }

        private NodeStatus GetBootStatus()
        {
            NodeStatus status = new NodeStatus();
            BusMasterStats stats = null;
            Read(reader =>
            {
                while (true)
                {
                    var code = reader.Read<Twocc>()?.Code;
                    switch (code)
                    {
                        case null: // EOF
                            status = null;
                            return;
                        case ResetCode:
                            status.ResetReason = (ResetReason) reader.Read<ushort>();
                            break;
                        case ExceptionText:
                            status.ExceptionMessage = reader.Read<DynamicString>().Str;
                            break;
                        case BusMasterStatitstics:
                            stats = reader.Read<BusMasterStats>();
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

            Logger.Log("Getting boot status: " + status);
            if (stats?.SocketTimeoutCount > 0)
            {
                Logger.Warning("Master Stats", "SocketTimeouts#", stats.SocketTimeoutCount);
            }

            if (status.ResetReason != ResetReason.None)
            {
                Write(writer =>
                {
                    writer.Write(SysCommand.ClearResetReason);
                });
            }
            return status;
        }

        /// <summary>
        /// Reset the device
        /// </summary>
        public async Task Reset()
        {
            Write(writer =>
            {
                writer.Write(SysCommand.Reset);
            });
        }
    }
}
