using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
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

    internal interface ITcpConnection : IConnectionReader, IConnectionWriter, IDisposable { }

    internal class TcpConnectionFactory : ServiceBase
    {
        private readonly Dictionary<IPEndPoint, ClientMutex> _mutexes = new Dictionary<IPEndPoint, ClientMutex>();
        private readonly object _lockObject = new object();
        private readonly TcpClientManager _clientManager = new TcpClientManager();

        private static readonly TimeSpan GRACE_TIME = TimeSpan.FromSeconds(10);

        private class ClientMutex
        {
            private readonly IPEndPoint _endPoint;
            private readonly TcpConnectionFactory _owner;
            private readonly Semaphore _mutex = new Semaphore(1, 1);
            private CancellationTokenSource _cancellationToken;

            public ClientMutex(IPEndPoint endPoint, TcpConnectionFactory owner)
            {
                _endPoint = endPoint;
                _owner = owner;
            }

            public void Acquire()
            {
                _mutex.WaitOne();
                // Ok, the client is alive
                if (_cancellationToken != null)
                {
                    _cancellationToken.Cancel();
                }
            }

            public void Release()
            {
                _cancellationToken = new CancellationTokenSource();

                // Start timeout auto-disposal timer
                Task.Delay(GRACE_TIME, _cancellationToken.Token).ContinueWith(task =>
                {
                    // Abruptly close the client
                    _owner.Abort(_endPoint);
                }, _cancellationToken.Token);

                _mutex.Release();
            }
        }

        public void Abort(IPEndPoint endPoint)
        {
            _clientManager.Abort(endPoint);
        }

        /// <summary>
        /// Can last less than a TcpClientManager.Client instance, but not more, spanning across multiple clients.
        /// </summary>
        private class TcpConnection : ITcpConnection
        {
            private readonly ClientMutex _mutex;
            private readonly IPEndPoint _endPoint;
            private readonly TcpClientManager _clientManager;
            private TcpClientManager.Client _client;
            private bool _disposed;

            public TcpConnection(IPEndPoint endPoint, ClientMutex mutex, TcpClientManager clientManager)
            {
                _endPoint = endPoint;
                _mutex = mutex;
                _clientManager = clientManager;
                // Locking
                _mutex.Acquire();
            }

            /// <summary>
            /// Release the TCP channel
            /// </summary>
            public void Dispose()
            {
                if (!_disposed)
                {
                    _mutex.Release();
                    _client = null;
                    _disposed = true;
                }
            }

            private TcpClientManager.Client Client
            {
                get
                {
                    if (_disposed)
                    {
                        return null;
                    }
                    // Lazy creation
                    if (_client == null)
                    {
                        _client = _clientManager.GetClient(_endPoint);
                        if (_client == null || _client.IsDisposed)
                        {
                            Dispose();
                        }
                    }
                    else if (_client.IsDisposed)
                    {
                        Dispose();
                    }
                    return _client;
                }
            }

            public Task Write<T>(T data)
            {
                return (Client?.Write(data)) ?? Task.CompletedTask;
            }

            public Task<T> Read<T>()
            {
                return (Client?.Read<T>()) ?? Task.FromResult(default(T));
            }
        }

        public ITcpConnection Create(IPEndPoint endPoint)
        {
            lock (_lockObject)
            {
                ClientMutex mutex;
                if (!_mutexes.TryGetValue(endPoint, out mutex))
                {
                    mutex = new ClientMutex(endPoint, this);
                    _mutexes[endPoint] = mutex;
                }

                return new TcpConnection(endPoint, mutex, _clientManager);
            }
        }
    }
}