using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Lucky.Home.Serialization;
using Lucky.Home.Services;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// Wrapper around the framework <see cref="TcpClient"/>. Exposes message-based read/write channel. 
    /// Not thread-safe.
    /// </summary>
    internal class MessageTcpClient
    {
        private static readonly TimeSpan NET_STREAM_TIMEOUT = TimeSpan.FromSeconds(5);

        private NetworkStream _stream;
        private readonly ILogger _logger;
        private TcpClient _tcpClient;
        public IPEndPoint EndPoint { get; private set; }

        public MessageTcpClient(IPEndPoint endPoint, ILogger logger)
        {
            _logger = logger;
            EndPoint = endPoint;

            _tcpClient = new TcpClient();
            // This can fail after 20 seconds in case of missing endpoint
            _tcpClient.Connect(endPoint);
            _tcpClient.SendTimeout = 1;
            _tcpClient.ReceiveTimeout = 1;

            _stream = _tcpClient.GetStream();

            // Make client to terminate if read stalls for more than 5 seconds (e.g. sink dead)
            _stream.ReadTimeout = _stream.WriteTimeout = (int)NET_STREAM_TIMEOUT.TotalMilliseconds;
        }

        public bool IsClosed
        {
            get
            {
                if (_tcpClient == null)
                {
                    return true;
                }
                if (!_tcpClient.Connected)
                {
                    _logger.Log("DEBUG:FoundNotConnected");
                    Close(false);
                }
                return _stream == null;
            }
        }

        public void Close(bool flush)
        {
            if (_stream != null)
            {
                var stream = _stream;
                var tcpClient = _tcpClient;
                if (flush)
                {
                    _stream.FlushAsync().ContinueWith(t1 =>
                    {
                        stream.Close();
                        stream.Dispose();
                        tcpClient?.Dispose();
                    });
                }
                else
                {
                    stream.Dispose();
                    tcpClient.Dispose();
                }
                _stream = null;
                _tcpClient = null;
            }
        }

        public async Task Write<T>(T data, bool flush)
        {
            try
            {
                await NetSerializer<T>.Write(_stream, data);
                if (flush)
                {
                    await _stream.FlushAsync();
                }
            }
            catch (Exception exc)
            {
                Exception src = exc.GetBaseException();
                if (src is SocketException || src is IOException || src is ObjectDisposedException)
                {
                    _logger.Log("Close:" + src.GetType().Name, "exc", exc.Message, "write", typeof(T).Name);
                }
                else
                {
                    _logger.Exception(new InvalidDataException("Exception writing object of type " + typeof(T).Name, exc));
                }
                // Destroy the channel
                Close(false);
            }
        }

        public async Task<T> Read<T>()
        {
            try
            {
                return await NetSerializer<T>.Read(_stream);
            }
            catch (Exception exc)
            {
                Exception src = exc.GetBaseException();
                if (src is SocketException || src is IOException)
                {
                    _logger.Log("Close:" + src.GetType().Name, "exc", exc.Message, "read", typeof(T).Name);
                }
                else
                {
                    _logger.Exception(new InvalidDataException("Exception reading object of type " + typeof(T).Name, exc));
                }
                // Destroy the channel
                Close(false);
                return default(T);
            }
        }
    }
}
