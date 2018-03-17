using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Lucky.Services;

// ReSharper disable once ClassNeverInstantiated.Global

namespace Lucky.Home.Protocol
{
    internal class TcpConnectionFactory : ServiceBase
    {
        private readonly Dictionary<IPEndPoint, ClientMutex> _mutexes = new Dictionary<IPEndPoint, ClientMutex>();
        private readonly object _lockObject = new object();
        private readonly TcpClientManager _clientManager = new TcpClientManager();

        private static readonly TimeSpan GRACE_TIME = TimeSpan.FromSeconds(10);

        internal class ClientMutex
        {
            private readonly IPEndPoint _endPoint;
            private readonly TcpConnectionFactory _owner;
            private readonly Mutex _mutex = new Mutex();
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

                _mutex.ReleaseMutex();
            }
        }

        public void Abort(IPEndPoint endPoint)
        {
            _clientManager.Abort(endPoint);
        }

        public TcpConnection Create(IPEndPoint endPoint)
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