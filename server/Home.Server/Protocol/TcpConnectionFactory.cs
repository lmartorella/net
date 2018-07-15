using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Lucky.Net;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    public class DeadlockException : ApplicationException
    {
        public DeadlockException(string message)
            :base(message)
        {

        }
    }

    /// <summary>
    /// Used for reader only
    /// </summary>
    public interface IConnectionReader
    {
        Task<T> Read<T>();
    }

    /// <summary>
    /// Used for writer only
    /// </summary>
    public interface IConnectionWriter
    {
        Task Write<T>(T data);
    }

    /// <summary>
    /// Interface of a tcp client wrapper. To dispose after each connection session.
    /// The underlying TCP session can be recycled.
    /// </summary>
    internal interface IConnectionSession : IConnectionReader, IConnectionWriter, IDisposable
    {
        /// <summary>
        /// Call this when the underneath tcp client socket should be closed instead of being reused for other connections (e.g. after errors)
        /// </summary>
        /// <param name="reason">For logging purpose</param>
        void Abort(string reason);
    }

    /// <summary>
    /// Manager for <see cref="IConnectionSession"/>
    /// </summary>
    internal class TcpConnectionFactory : ServiceBase
    {
        private static readonly TimeSpan GRACE_TIME = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan MAX_LIFE_TIME = TimeSpan.FromMinutes(30);

        private readonly object _lockObject = new object();
        private Dictionary<IPEndPoint, Connection> _connections = new Dictionary<IPEndPoint, Connection>();

        /// <summary>
        /// Alive TCP connection
        /// </summary>
        private class Connection
        {
            private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
            private CancellationTokenSource _cancellationToken;
            private CancellationTokenSource _lockCancellationToken;
            private bool _timedOut;

            public IClient Client { get; private set; }
            private readonly ILogger _logger;
            private readonly DateTime _openTime;

            public Connection(IClient client, ILogger logger)
            {
                Client = client;
                _logger = logger;
                _openTime = DateTime.Now;
            }

            public void Close(bool flush)
            {
                Client.Close(flush);
            }

            public void Acquire()
            {
                // If 30 seconds elapses, a deadlock occurred somewhere...
                if (!_semaphore.Wait(30000))
                {
                    Close(false);
                    throw new DeadlockException("Acquire locked");
                }

                // Ok, the client is alive
                if (_cancellationToken != null)
                {
                    _cancellationToken.Cancel();
                    _cancellationToken = null;
                }

                // Start a deadlock timer
                _lockCancellationToken = new CancellationTokenSource();
                Task.Delay(25000, _lockCancellationToken.Token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        _logger.Log("MissingRelease", "EP", Client.EndPoint);
                        Close(false);
                    }
                });
            }

            public void Release()
            {
                _lockCancellationToken.Cancel();
                if (_timedOut)
                {
                    // Print the stack to understand why GraceTime triggered
                    _logger.Exception(new InvalidOperationException("TimedOut (noexc)"));
                }

                // Create new cancellation token
                _cancellationToken = new CancellationTokenSource();

                // Start timeout auto-disposal timer
                Task.Delay(GRACE_TIME, _cancellationToken.Token).ContinueWith(t =>
                {
                    if (!t.IsCanceled && !Client.IsClosed)
                    {
                        _logger.Log("GraceTime", "EP", Client.EndPoint);
                        _timedOut = true;
                        Close(false);
                    }
                });

                if (DateTime.Now > (_openTime + MAX_LIFE_TIME))
                {
                    // Close session
                    _logger.Log("DEBUG:MaxLive", "EP", Client.EndPoint);
                    Close(true);
                }
                _semaphore.Release();
            }
        }

        private class ConnectionSession : IConnectionSession
        {
            private bool _disposed;
            private IClient _client;
            private readonly Action<string> _abort;
            private string _abortReason;

            public ConnectionSession(IClient client, Action<string> abort)
            {
                _client = client;
                _abort = abort;
            }

            /// <summary>
            /// Release the TCP channel
            /// </summary>
            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _abort(_abortReason);
                }
            }

            private bool IsClosed()
            {
                return _disposed || _client.IsClosed;
            }

            public Task Write<T>(T data)
            {
                if (IsClosed())
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
                if (IsClosed())
                {
                    return Task.FromResult(default(T));
                }
                else
                {
                    return _client.Read<T>();
                }
            }

            public void Abort(string reason)
            {
                _abortReason = reason;
            }
        }

        public IConnectionSession GetConnection(IPEndPoint endPoint)
        {
            Connection connection;
            // Check for existing connection
            lock (_lockObject)
            {
                // Spin, possible to receive a closed session when in queue
                do
                {
                    if (!_connections.TryGetValue(endPoint, out connection))
                    {
                        connection = CreateConnection(endPoint);
                        if (connection == null)
                        {
                            // Connection cannot be made
                            return null;
                        }
                        // Ok connection working
                        _connections[endPoint] = connection;
                    }

                    // The recycled connection in the map can be closed. In that case remove it
                    if (connection.Client.IsClosed)
                    {
                        _connections.Remove(endPoint);
                        connection = null;
                    }
                } while (connection == null);
            }

            // Acquire it outside the lock
            connection.Acquire();

            // Ok the connection is free to use
            return new ConnectionSession(connection.Client, abortReason =>
            {
                lock (_lockObject)
                {
                    connection.Release();
                    if (abortReason != null)
                    {
                        Logger.Log("Close", "reason", abortReason, "EP", endPoint);
                        connection.Close(false);
                    }
                    if (connection.Client.IsClosed)
                    {
                        _connections.Remove(endPoint);
                    }
                }
            });
        }

        /// <summary>
        /// Can return null if connection cannot be established
        /// </summary>
        private Connection CreateConnection(IPEndPoint endPoint)
        {
            IClient client;
            // Destroy the channel
            DateTime start = DateTime.Now;
            try
            {
                client = Manager.GetService<TcpService>().CreateClient(endPoint);
                Logger.Log("DEBUG:NewClient", "EP", endPoint);
                return new Connection(client, Logger);
            }
            catch (SocketException exc)
            {
                TimeSpan elapsed = DateTime.Now - start;
                // Cannot connect
                Logger.Log("Cannot Connect", "EP", endPoint, "code:exc", exc.ErrorCode + ":" + exc.Message, "elapsed(ms)", elapsed.TotalMilliseconds);
                return null;
            }
        }
    }
}