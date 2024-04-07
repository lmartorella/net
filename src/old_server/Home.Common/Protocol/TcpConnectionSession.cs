using System;
using System.Threading.Tasks;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// A disposable session on a TCP message-based stream. To dispose when the session is terminated. 
    /// Not-thread safe.
    /// The underlying TCP session <see cref="MessageTcpClient"/> is not closed, since it is recycled by subsequent sessions until max life time.
    /// Each instance will run in mutex between instances on the same TCP socket.
    /// Support abortion, that causes the underneath TCP socket to be terminated in case of protocol errors.
    /// </summary>
    internal class TcpConnectionSession : IConnectionReader, IConnectionWriter, IDisposable
    {
        private bool _disposed;
        private MessageTcpClient _client;
        private string _abortReason;

        public TcpConnectionSession(MessageTcpClient client)
        {
            _client = client;
        }

        public class DisposedEventArgs : EventArgs 
        {
            /// <summary>
            /// If not null, the socket should be aborted, closed and not reused.
            /// </summary>
            public readonly string IsAborted;

            public DisposedEventArgs(string isAborted)
            {
                IsAborted = isAborted;
            }
        }

        /// <summary>
        /// Event raised when the session is ended.
        /// </summary>
        public event EventHandler<DisposedEventArgs> Disposed;

        /// <summary>
        /// Release the TCP channel
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Disposed?.Invoke(this, new DisposedEventArgs(_abortReason));
            }
        }

        private bool IsClosed
        {
            get
            {
                return _disposed || _client.IsClosed;
            }
        }

        public Task Write<T>(T data)
        {
            if (IsClosed)
            {
                return Task.CompletedTask;
            }
            else
            {
                return _client.Write(data, true);
            }
        }

        public Task<T> Read<T>()
        {
            if (IsClosed)
            {
                return Task.FromResult(default(T));
            }
            else
            {
                return _client.Read<T>();
            }
        }

        /// <summary>
        /// Call this when the underneath tcp client socket should be closed instead of being reused for other connections (e.g. after errors)
        /// </summary>
        /// <param name="reason">For logging purpose</param>
        public void Abort(string reason)
        {
            _abortReason = reason;
        }
    }
}