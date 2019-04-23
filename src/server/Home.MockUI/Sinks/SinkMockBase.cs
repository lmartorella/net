using Lucky.HomeMock.Core;
using System;
using System.IO;
using System.Text;

namespace Lucky.HomeMock.Sinks
{
    abstract class SinkMockBase
    {
        public readonly string FourCc;

        protected SinkMockBase(string fourCc)
        {
            FourCc = fourCc;
        }

        public abstract void Read(BinaryReader reader);
        public abstract void Write(BinaryWriter writer);

        protected void WriteTwocc(BinaryWriter writer, string code)
        {
            var bytes = Encoding.ASCII.GetBytes(code);
            if (bytes.Length != 2)
            {
                throw new ArgumentException("code");
            }
            writer.Write(bytes, 0, 2);
        }

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

        /// <summary>
        /// For logging
        /// </summary>
        public event EventHandler<ItemEventArgs<string>> LogLine;

        protected void Log(string str)
        {
            LogLine?.Invoke(this, new ItemEventArgs<string>(str));
        }
    }
}