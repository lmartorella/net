using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Lucky.Serialization;
using Lucky.Services;

namespace Lucky.Net
{
    public interface IClient
    {
        void Close(bool flush);
        bool IsClosed { get; }
        IPEndPoint EndPoint { get; }

        Task Write<T>(T data, bool flush);
        Task<T> Read<T>();
    }

    /// <summary>
    /// Helper class
    /// </summary>
    public class TcpService : ServiceBase
    {
        private static readonly TimeSpan NET_STREAM_TIMEOUT = TimeSpan.FromSeconds(5);

        private TcpListener TryCreateListener(IPAddress address, ref int startPort)
        {
            do
            {
                try
                {
                    return new TcpListener(address, startPort);
                }
                catch (SocketException)
                {
                    startPort++;
                    Logger.Log("TCPPortBusy", "port", address + ":" + startPort, "trying", startPort);
                }
            } while (true);
        }

        public TcpListener CreateListener(IPAddress address, int startPort, string portName, Action<NetworkStream> incomingHandler)
        {
            var listener = TryCreateListener(address, ref startPort);
            Logger.Log("Opened", "socket", portName, "address", address + ":" + startPort);

            listener.Start();
            AsyncCallback handler = null;
            handler = ar =>
            {
                var tcpClient = listener.EndAcceptTcpClient(ar);
                Task.Run(() =>
                {
                    try
                    {
                        incomingHandler(tcpClient.GetStream());
                    }
                    catch (Exception exc)
                    {
                        Logger.Exception(exc);
                    }
                });
                listener.BeginAcceptTcpClient(handler, null);
            };
            listener.BeginAcceptTcpClient(handler, null);
            return listener;
        }

        /// <summary>
        /// A TCP client
        /// </summary>
        private class Client : IClient
        {
            private NetworkStream _stream;
            private readonly ILogger _logger;
            private TcpClient _tcpClient;
            private readonly object _lockObject = new object();
            public IPEndPoint EndPoint { get; private set; }

            public Client(TcpClient tcpClient, IPEndPoint endPoint, ILogger logger)
            {
                _logger = logger;
                _tcpClient = tcpClient;
                EndPoint = endPoint;
                _stream = _tcpClient.GetStream();

                // Make client to terminate if read stalls for more than 5 seconds (e.g. sink dead)
                _stream.ReadTimeout = _stream.WriteTimeout = (int)NET_STREAM_TIMEOUT.TotalMilliseconds;
            }

            public bool IsClosed
            {
                get
                {
                    lock (_lockObject)
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
            }

            public void Close(bool flush)
            {
                lock (_lockObject)
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

        public IClient CreateClient(IPEndPoint endPoint)
        {
            var tcpClient = new TcpClient();
            // This can fail after 20 seconds in case of missing endpoint
            tcpClient.Connect(endPoint);
            tcpClient.SendTimeout = 1;
            tcpClient.ReceiveTimeout = 1;

            return new Client(tcpClient, endPoint, Logger);
        }
    }
}
