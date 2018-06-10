using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Lucky.Net;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    public interface IConnectionReader
    {
        Task<T> Read<T>();
    }

    public interface IConnectionWriter
    {
        Task Write<T>(T data);
    }

    internal interface ITcpConnection : IConnectionReader, IConnectionWriter, IDisposable
    {
    }

    internal class TcpConnectionFactory : ServiceBase
    {
        private readonly object _lockObject = new object();
        /// <summary>
        /// TCP clients (Cached when not used)
        /// </summary>
        private readonly Dictionary<IPEndPoint, IClient> _clients = new Dictionary<IPEndPoint, IClient>();

        /// <summary>
        /// Active client connections
        /// </summary>
        private readonly Dictionary<IPEndPoint, ClientMutex> _mutexes = new Dictionary<IPEndPoint, ClientMutex>();

        private static readonly TimeSpan GRACE_TIME = TimeSpan.FromSeconds(10);

        private class ClientMutex
        {
            private Action _abortFunction;
            private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
            private CancellationTokenSource _cancellationToken;

            public ClientMutex(Action abortFunction)
            {
                _abortFunction = abortFunction;
            }

            public void Dispose()
            {
                //_semaphore.Dispose();
            }

            public async Task AcquireAsync()
            {
                await _semaphore.WaitAsync();
                // Ok, the client is alive
                if (_cancellationToken != null)
                {
                    _cancellationToken.Cancel();
                    _cancellationToken = null;
                }
            }

            public void Release()
            {
                _cancellationToken = new CancellationTokenSource();

                // Start timeout auto-disposal timer                {
                Task.Delay(GRACE_TIME, _cancellationToken.Token).ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        _abortFunction();
                    }
                });

                try
                {
                    _semaphore.Release();
                }
                catch (ObjectDisposedException)
                {

                }
            }
        }

        /// <summary>
        /// Can last less than a TcpService.IClient instance, but not more, spanning across multiple clients.
        /// </summary>
        private class TcpConnection : ITcpConnection
        {
            private readonly ClientMutex _mutex;
            private readonly IPEndPoint _endPoint;
            private readonly TcpConnectionFactory _owner;
            private IClient _client;
            private bool _disposed;

            public TcpConnection(IPEndPoint endPoint, ClientMutex mutex, TcpConnectionFactory clientManager)
            {
                _endPoint = endPoint;
                _mutex = mutex;
                _owner = clientManager;
            }

            /// <summary>
            /// Release the TCP channel
            /// </summary>
            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    _mutex.Release();
                    _client = null;
                }
            }

            private IClient GetClient()
            {
                if (_disposed)
                {
                    return null;
                }
                // Lazy creation
                if (_client == null)
                {
                    _client = _owner.GetClient(_endPoint);
                    if (_client == null || _client.IsClosed)
                    {
                        Dispose();
                    }
                }
                else if (_client.IsClosed)
                {
                    Dispose();
                    _client = null;
                }
                return _client;
            }

            public Task Write<T>(T data)
            {
                return (GetClient()?.Write(data, true)) ?? Task.CompletedTask;
            }

            public Task<T> Read<T>()
            {
                return (GetClient()?.Read<T>()) ?? Task.FromResult(default(T));
            }
        }

        public async Task<ITcpConnection> Create(IPEndPoint endPoint)
        {
            ClientMutex mutex;
            lock (_lockObject)
            {
                if (!_mutexes.TryGetValue(endPoint, out mutex))
                {
                    mutex = new ClientMutex(() => Abort(endPoint));
                    _mutexes[endPoint] = mutex;
                }
            }
            // Acquire mutex
            await mutex.AcquireAsync();
            return new TcpConnection(endPoint, mutex, this);
        }

        private IClient GetClient(IPEndPoint endPoint)
        {
            lock (_lockObject)
            {
                IClient client;
                if (!_clients.TryGetValue(endPoint, out client))
                {
                    // Destroy the channel
                    DateTime start = DateTime.Now;
                    try
                    {
                        client = Manager.GetService<TcpService>().CreateClient(endPoint, () => Abort(endPoint));
                        _clients[endPoint] = client;
                    }
                    catch (SocketException exc)
                    {
                        TimeSpan elapsed = DateTime.Now - start;
                        // Cannot connect
                        Logger.Log("Cannot Connect", "EP", endPoint, "code:exc", exc.ErrorCode + ":" + exc.Message, "elapsed(ms)", elapsed.TotalMilliseconds);
                        client = null;
                    }
                }
                return client;
            }
        }

        public void Abort(IPEndPoint endPoint)
        {
            IClient client = null;
            lock (_lockObject)
            {
                if (_clients.TryGetValue(endPoint, out client))
                {
                    // Destroy the channel
                    _clients.Remove(endPoint);
                }
                ClientMutex mutex = null;
                if (_mutexes.TryGetValue(endPoint, out mutex))
                {
                    // Remove the mutex
                    _mutexes.Remove(endPoint);
                    mutex.Dispose();
                }
            }
            if (client != null)
            {
                client.Close();
            }
        }
    }
}