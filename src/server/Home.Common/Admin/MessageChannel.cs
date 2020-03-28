using Lucky.Home.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Lucky.Home.Admin
{
    /// <summary>
    /// Bidirectional message-based channel 
    /// </summary>
    class MessageChannel
    {
        private readonly Stream _stream;

        public MessageChannel(Stream stream)
        {
            _stream = stream;
        }

        public async Task WriteMessage(byte[] buffer)
        {
            await _stream.SafeWriteAsync(buffer.Concat(new byte[1] { 0 }).ToArray(), buffer.Length + 1);
        }

        public async Task<byte[]> ReadMessage()
        {
            List<byte> buffer = new List<byte>(128);
            do
            {
                byte[] ch = new byte[1];
                int l = await _stream.SafeReadAsync(ch, 1);
                if (l < 1)
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
    }
}
