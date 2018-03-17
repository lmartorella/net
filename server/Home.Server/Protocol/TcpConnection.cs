using System;
using System.Net;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// Can last less than a TcpClientManager.Client instance.
    /// </summary>
    internal class TcpConnection : IDisposable, IConnectionReader, IConnectionWriter
    {
        private TcpConnectionFactory.ClientMutex _mutex;
        private readonly IPEndPoint _endPoint;
        private readonly TcpClientManager _clientManager;
        private TcpClientManager.Client _client;
        private bool _disposed;

        public TcpConnection(IPEndPoint endPoint, TcpConnectionFactory.ClientMutex mutex, TcpClientManager clientManager)
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
            if (_mutex != null)
            {
                _mutex.Release();
                _mutex = null;
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
                if (_client == null)
                {
                    _client = _clientManager.GetClient(_endPoint);
                }
                return (_client == null || _client.IsDisposed) ? null : _client;
            }
        }

        public void Write<T>(T data)
        {
            Client?.Write(data);
        }

        public T Read<T>()
        {
            var client = Client;
            return client != null ? client.Read<T>() : default(T);
        }

        public byte[] ReadBytes(int byteCount)
        {
            return Client?.ReadBytes(byteCount);
        }

        public void WriteBytes(byte[] bytes)
        {
            Client?.WriteBytes(bytes);
        }
    }
}