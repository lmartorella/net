using System;
using System.Net;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// Can last more than a TcpClientManager.Client instance
    /// </summary>
    internal class TcpConnection : IDisposable, IConnectionReader, IConnectionWriter
    {
        private TcpConnectionFactory.ClientMutex _mutex;
        private readonly IPEndPoint _endPoint;
        private readonly TcpClientManager _clientManager;

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
            }
        }

        public void Write<T>(T data)
        {
            _clientManager.GetClient(_endPoint).Write(data);
        }

        public T Read<T>()
        {
            return _clientManager.GetClient(_endPoint).Read<T>();
        }

        public byte[] ReadBytes(int byteCount)
        {
            return _clientManager.GetClient(_endPoint).ReadBytes(byteCount);
        }

        public void WriteBytes(byte[] bytes)
        {
            _clientManager.GetClient(_endPoint).WriteBytes(bytes);
        }
    }
}