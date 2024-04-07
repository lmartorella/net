using System;
using System.IO;
using System.Threading.Tasks;

namespace Lucky.Home.IO
{
    /// <summary>
    /// Extension for Stream async IO to manage exceptions
    /// </summary>
    public static class StreamExt
    {
        public static async Task<int> SafeReadAsync(this Stream stream, byte[] buffer, int count)
        {
            try 
            {
                return await stream.ReadAsync(buffer, 0, count);
            }
            catch (ObjectDisposedException)
            {
                // Channel closed or disposed
                return 0;
            }
        }

        public static async Task<int> SafeWriteAsync(this Stream stream, byte[] buffer, int count)
        {
            try
            {
                await stream.WriteAsync(buffer, 0, count);
                return count;
            }
            catch (ObjectDisposedException)
            {
                // Channel closed or disposed
                return 0;
            }
        }
    }
}
