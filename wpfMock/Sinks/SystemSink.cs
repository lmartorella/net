using System;
using System.IO;
using Lucky.Home;

namespace Lucky.HomeMock.Sinks
{
    internal class SystemSink : SinkBase
    {
        public SystemSink()
            : base("SYS ")
        {
            ResetReason = ResetReason.Power;
        }

        public ResetReason ResetReason { get; set; }
        public string ExcMsg { get; set; }

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