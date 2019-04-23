using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Lucky.IO
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
            await _stream.WriteAsync(buffer, 0, buffer.Length);
            // Message separator
            _stream.WriteByte(0);
        }

        public async Task<byte[]> ReadMessage()
        {
            List<byte> buffer = new List<byte>(128);
            do
            {
                byte[] ch = new byte[1];
                int l;
                try
                {
                    l = await _stream.ReadAsync(ch, 0, 1);
                }
                catch (Exception)
                {
                    // EOF/closed
                    return null;
                }
                if (l == 0)
                {
                    // EOF
                    return null;
                }
                if (ch[0] == 0)
                {
                    return buffer.ToArray();
                }
                buffer.Add(ch[0]);
            } while (true);
        }

        public void Dispose()
        {
            //_stream.Dispose();
        }
    }
}
