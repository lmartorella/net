using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Lucky.Home.Serialization;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    public interface IConnectionReader
    {
        T Read<T>();
    }

    public interface IConnectionWriter
    {
        void Write<T>(T data);
    }

    internal class TcpConnection : IDisposable, IConnectionReader, IConnectionWriter
    {
        private static readonly Dictionary<IPEndPoint, Client> s_instances = new Dictionary<IPEndPoint, Client>();
        private Client _client;
        private static readonly TimeSpan GRACE_TIME = TimeSpan.FromSeconds(10);
        private static readonly ILogger Logger = Manager.GetService<LoggerFactory>().Create("NetSerializer");

        private class CloseMessage
        {
            public Fourcc Cmd = new Fourcc("CLOS");
        }

        private class Client
        {
            private readonly IPEndPoint _endPoint;
            private TcpClient _tcpClient;
            private readonly Stream _stream;
            private readonly BinaryReader _reader;
            private readonly Mutex _semaphore = new Mutex();
            private CancellationTokenSource _cancellationToken;

            public Client(IPEndPoint endPoint)
            {
                _endPoint = endPoint;
                _tcpClient = new TcpClient();
                _tcpClient.Connect(endPoint);
                _tcpClient.NoDelay = true;
                _stream = _tcpClient.GetStream();
                //Stream.WriteTimeout = 5 * 60 * 1000;
                _reader = new BinaryReader(_stream);
                _stream.ReadTimeout = 5000;
                //Stream.WriteTimeout = 5 * 60 * 1000;
            }

            public void Dispose(bool destroying)
            {
                lock (this)
                {
                    if (_tcpClient != null)
                    {
                        if (!destroying)
                        {
                            SendClose();
                        }

                        _stream.Flush();
                        _reader.Close();
                        //_tcpClient.Close();
                        _tcpClient = null;
                    }
                }
            }

            public void Acquire()
            {
                _semaphore.WaitOne();
                lock (this)
                {
                    if (_cancellationToken != null)
                    {
                        _cancellationToken.Cancel();
                    }
                }
            }

            public void Release()
            {
                lock (this)
                {
                    _cancellationToken = new CancellationTokenSource();
                    _semaphore.ReleaseMutex();
                    // Start timeout auto-disposal timer
                    Task.Delay(GRACE_TIME, _cancellationToken.Token).ContinueWith(task =>
                    {
                        lock (s_instances)
                        {
                            Dispose(false);
                            s_instances.Remove(_endPoint);
                        }
                    }, _cancellationToken.Token);
                }
            }

            public void Write<T>(T data)
            {
                try
                {
                    byte[] raw = data as byte[];
                    if (raw != null)
                    {
                        _stream.Write(raw, 0, raw.Length);
                    }
                    else
                    {
                        MemoryStream stream = new MemoryStream();
                        var writer = new BinaryWriter(stream);
                        NetSerializer<T>.Write(data, writer);
                        writer.Flush();
                        byte[] data1 = stream.ToArray();
                        stream.Position = 0;
                        _stream.Write(data1, 0, data1.Length);
                    }
                    _stream.Flush();
                }
                catch (Exception exc)
                {
                    Logger.Exception(new InvalidDataException("Exception writing object of type " + typeof(T).Name, exc));
                    // Destroy the channel
                    Close(_endPoint);
                }
            }

            public T Read<T>()
            {
                try
                {
                    return NetSerializer<T>.Read(_reader);
                }
                catch (Exception exc)
                {
                    Logger.Exception(new InvalidDataException("Exception reading object of type " + typeof(T).Name, exc));
                    // Destroy the channel
                    Close(_endPoint);
                    return default(T);
                }
            }

            public void SendClose()
            {
                Write(new CloseMessage());
            }
        }

        public TcpConnection(IPAddress address, ushort port)
        {
            IPEndPoint endPoint = new IPEndPoint(address, port);
            lock (s_instances)
            {
                if (!s_instances.TryGetValue(endPoint, out _client))
                {
                    _client = new Client(endPoint);
                    s_instances[endPoint] = _client;
                }
            }
            // Locking
            _client.Acquire();
        }

        public void Write<T>(T data)
        {
            _client.Write(data);
        }

        public T Read<T>()
        {
            return _client.Read<T>();
        }

        /// <summary>
        /// Close the TCP client connection
        /// </summary>
        public void Dispose()
        {
            if (_client != null)
            {
                _client.Release();
                _client = null;
            }
        }

        public static void Close(IPEndPoint endPoint)
        {
            lock (s_instances)
            {
                Client client;
                if (s_instances.TryGetValue(endPoint, out client))
                {
                    // Destroy the channel
                    client.Dispose(true);
                    s_instances.Remove(endPoint);
                }
            }
        }
    }
}