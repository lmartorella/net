using System;
using System.IO;
using System.Text;

namespace Lucky.HomeMock.Sinks
{
    abstract class SinkBase
    {
        public readonly string FourCc;

        protected SinkBase(string fourCc)
        {
            FourCc = fourCc;
        }

        public abstract void Read(BinaryReader reader);
        public abstract void Write(BinaryWriter writer);

        protected void WriteFourcc(BinaryWriter writer, string code)
        {
            var bytes = Encoding.ASCII.GetBytes(code);
            if (bytes.Length != 4)
            {
                throw new ArgumentException("code");
            }
            writer.Write(bytes, 0, 4);
        }

        protected void WriteString(BinaryWriter writer, string str)
        {
            var buffer = Encoding.ASCII.GetBytes(str);
            writer.Write((ushort)buffer.Length);
            writer.Write(buffer);
        }
    }
}