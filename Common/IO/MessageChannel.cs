using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Lucky.Home.IO
{
    public class MessageChannel : IDisposable
    {
        private readonly Stream _stream;

        public MessageChannel(Stream stream)
        {
            _stream = stream;
        }

        public async Task WriteMessage(byte[] buffer)
        {
            await Task.Run(() =>
            {
                _stream.Write(buffer, 0, buffer.Length);
                // Message separator
                _stream.WriteByte(0);
            });
        }

        public async Task<byte[]> ReadMessage()
        {
            return await Task.Run(() =>
            {
                List<byte> buffer = new List<byte>(128);
                do
                {
                    byte ch = (byte)_stream.ReadByte();
                    if (ch == 0)
                    {
                        return buffer.ToArray();
                    }
                    buffer.Add(ch);
                } while (true);
            });
        }

        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
