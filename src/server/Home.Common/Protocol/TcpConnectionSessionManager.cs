using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Lucky.Net;
using Lucky.Home.Services;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// Manager for <see cref="TcpConnectionSession"/>. 
    /// Maintain underlying TCP sockets connections alive to avoid too much IP traffic in opening/closing sockets, and save resources
    /// on basic MCU nodes.
    /// </summary>
    internal class TcpConnectionSessionManager : ServiceBase
    {
        private static readonly TimeSpan GRACE_TIME = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Don't let a single TCP socket to run too long. After 30 minutes forces a closing and reopening of the underlying socket.
        /// </summary>
        private static readonly TimeSpan MAX_LIFE_TIME = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Max time to wait for a <see cref="TcpConnectionSession"/> acquisition.
        /// </summary>
        private static readonly TimeSpan ACQUISITION_TIMEOUT = TimeSpan.FromSeconds(30);

        /// <summary>
        /// If acquire is longer that this, log a warning
        /// </summary>
        private static readonly TimeSpan SLOW_ACQUIRE_LOG = TimeSpan.FromSeconds(5);

        /// <summary>
        /// If a session remains locked for more than 25 seconds (close to the timeout), log a error
        /// </summary>
        private static readonly TimeSpan LONG_SESSION = TimeSpan.FromSeconds(25);

        /// <summary>
        /// Collects all connection, keyed by remote IP end-point (unique, since it uses same client connection for many sessions in mutex)
        /// </summary>
        private Dictionary<IPEndPoint, Connection> _connections = new Dictionary<IPEndPoint, Connection>();

        /// <summary>
        /// References an alive <see cref="MessageTcpClient"/> connection, and implement mutex logic.
        /// </summary>
        private class Connection
        {
            /// <summary>
            /// No need for inter-process semaphores, use slim
            /// </summary>
            private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

            /// <summary>
            /// Token for grace time
            /// </summary>
            private CancellationTokenSource _cancellationToken;

            /// <summary>
            /// Token for log errors in case of long sessions
            /// </summary>
            private CancellationTokenSource _missingReleaseTimeoutTokenSource;

            public MessageTcpClient Client { get; private set; }
            private readonly ILogger _logger;
            private readonly DateTime _openTime;

            public Connection(MessageTcpClient client, ILogger logger)
            {
                Client = client;
                _logger = logger;
                _openTime = DateTime.Now;
            }

            /// <summary>
            /// Thread safe
            /// </summary>
            public void Acquire()
            {
                DateTime ts = DateTime.Now;
                // If 30 seconds elapses, a deadlock occurred somewhere...
                if (!_semaphore.Wait(ACQUISITION_TIMEOUT))
                {
                    Client.Close(false);
                    throw new DeadlockException("Acquire locked");
                }

                var elapsed = DateTime.Now - ts;
                if (elapsed > SLOW_ACQUIRE_LOG)
                {
                    _logger.Log("WARN", "slowAcquire", elapsed.TotalSeconds + "s", "IP", Client.EndPoint);
                }

                // Ok, the client is used again. Cancel the auto-close timeout
                _cancellationToken?.Cancel();

                // Start a timer to log errors in case of missing release
                _missingReleaseTimeoutTokenSource = new CancellationTokenSource();
                Task.Delay(LONG_SESSION, _missingReleaseTimeoutTokenSource.Token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        _logger.Log("MissingRelease", "EP", Client.EndPoint);
                        // Causes readers to fail
                        Client.Close(false);
                    }
                });
            }

            public void Release()
            {
                // Cancel the missing release error
                _missingReleaseTimeoutTokenSource.Cancel();

                // Start timeout auto-disposal timer if not re-acquired in 10 seconds
                _cancellationToken = new CancellationTokenSource();
                Task.Delay(GRACE_TIME, _cancellationToken.Token).ContinueWith(t =>
                {
                    if (!t.IsCanceled && !Client.IsClosed)
                    {
                        _logger.Log("GraceTime", "EP", Client.EndPoint);
                        Client.Close(false);
                    }
                });

                if ((DateTime.Now - _openTime) > MAX_LIFE_TIME)
                {
                    // Close session
                    _logger.Log("DEBUG:MaxLive", "EP", Client.EndPoint);
                    Client.Close(true);
                }
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Get a working connection session from the pool.
        /// Can return null if connection cannot be established due to <see cref="SocketException"/>.
        /// </summary>
        internal TcpConnectionSession GetConnection(IPEndPoint endPoint)
        {
            // Until a connection is returned, create a queue
            lock (this)
            {
                while (true)
                {
                    Connection connection;
                    // Check for existing connection
                    lock (_connections)
                    {
                        if (!_connections.TryGetValue(endPoint, out connection))
                        {
                            connection = CreateConnection(endPoint);
                            if (connection == null)
                            {
                                // Connection cannot be made at this moment
                                return null;
                            }
                            // Ok store it.
                            _connections[endPoint] = connection;
                        }
                    }

                    Action<string> release = isAborted =>
                    {
                        // Release is thread-safe
                        connection.Release();
                        lock (_connections)
                        {
                            if (isAborted != null)
                            {
                                Logger.Log("Close", "reason", isAborted, "EP", endPoint);
                                connection.Client.Close(false);
                            }
                            if (connection.Client.IsClosed)
                            {
                                _connections.Remove(endPoint);
                            }
                        }
                    };

                    // Acquire it outside the hashtable lock, since this operation can be blocking and we should wait for releases.
                    // The Acquire is thread-safe
                    connection.Acquire();
                    if (connection.Client.IsClosed)
                    {
                        // In case of channel closed (e.g. for timeout), release it and abort it
                        release("acquire");
                        // Retry connection again
                        continue;
                    }

                    // Ok the connection is free to use now
                    var ret = new TcpConnectionSession(connection.Client);
                    ret.Disposed += (_, e) => release(e.IsAborted);
                    return ret;
                }
            }
        }

        /// <summary>
        /// Create a fresh new mutex connection.
        /// Can return null if connection cannot be established due to <see cref="SocketException"/>
        /// </summary>
        private Connection CreateConnection(IPEndPoint endPoint)
        {
            DateTime start = DateTime.Now;
            try
            {
                var client = Manager.GetService<TcpService>().CreateClient(endPoint);
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