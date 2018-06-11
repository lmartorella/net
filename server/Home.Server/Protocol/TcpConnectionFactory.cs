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
        void Close(string reason);
    }

    /// <summary>
    /// Manager for <see cref="IConnectionSession"/>
    /// </summary>
    internal class TcpConnectionFactory : ServiceBase
    {
        private static readonly TimeSpan GRACE_TIME = TimeSpan.FromSeconds(10);
        private readonly object _lockObject = new object();
        private Dictionary<IPEndPoint, Connection> _connections = new Dictionary<IPEndPoint, Connection>();

        /// <summary>
        /// Alive TCP connection
        /// </summary>
        private class Connection
        {
            private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
            private CancellationTokenSource _cancellationToken;
            public IClient Client { get; private set; }
            private readonly ILogger _logger;

            public Connection(IClient client, ILogger logger)
            {
                Client = client;
                _logger = logger;
            }

            public void Close()
            {
                Client.Close();
            }

            public void Acquire()
            {
                _semaphore.Wait();
                // Ok, the client is alive
                if (_cancellationToken != null)
                {
                    _cancellationToken.Cancel();
                    _cancellationToken = null;
                }
            }

            public void Release()
            {
                // Create new cancellation token
                _cancellationToken = new CancellationTokenSource();

                // Start timeout auto-disposal timer                {
                Task.Delay(GRACE_TIME, _cancellationToken.Token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        _logger.Log("DEBUG:GraceTime", "EP", Client.EndPoint);
                        Close();
                    }
                });

                _semaphore.Release();
            }
        }

        private class ConnectionSession : IConnectionSession
        {
            private bool _disposed;
            private IClient _client;
            private readonly Action<string> _release;
            private string _closedReason;

            public ConnectionSession(IClient client, Action<string> release)
            {
                _client = client;
                _release = release;
            }

            /// <summary>
            /// Release the TCP channel
            /// </summary>
            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _release(_closedReason);
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

            public void Close(string reason)
            {
                _closedReason = reason;
            }
        }

        public IConnectionSession GetConnection(IPEndPoint endPoint)
        {
            Connection connection;
            // Spin, possible to receive a closed session when in queue
            do
            {
                // Check for existing connection
                lock (_lockObject)
                {
                    if (!_connections.TryGetValue(endPoint, out connection))
                    {
                        connection = CreateConnection(endPoint);
                        if (connection == null)
                        {
                            // Connection cannot be made
                            return null;
                        }
                        _connections[endPoint] = connection;
                    }
                }

                // Acquire it
                connection.Acquire();
            } while (connection.Client.IsClosed);

            // Ok the connection is free to use
            return new ConnectionSession(connection.Client, closeReason =>
            {
                lock (_lockObject)
                {
                    connection.Release();
                    if (closeReason != null)
                    {
                        Logger.Log("Close", "reason", closeReason);
                        connection.Close();
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