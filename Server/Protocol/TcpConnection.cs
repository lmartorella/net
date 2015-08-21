using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Lucky.Home.Serialization;

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
        private static readonly TimeSpan GRACE_TIME = TimeSpan.FromSeconds(4);

        private class CloseMessage
        {
            public Fourcc Cmd = new Fourcc("CLOS");
        }

        private class Client
        {
            private readonly IPEndPoint _endPoint;
            private TcpClient _tcpClient;
            public readonly Stream Stream;
            public readonly BinaryReader Reader;
            private readonly Semaphore _semaphore = new Semaphore(1, 1);
            private CancellationTokenSource _cancellationToken;

            public Client(IPEndPoint endPoint)
            {
                _endPoint = endPoint;
                _tcpClient = new TcpClient();
                _tcpClient.Connect(endPoint);
                Stream = _tcpClient.GetStream();
                //Stream.ReadTimeout = 5 * 60 * 1000;
                //Stream.WriteTimeout = 5 * 60 * 1000;
                Reader = new BinaryReader(Stream);
            }

            public void Dispose(bool destroying)
            {
                lock (this)
                {
                    if (_tcpClient != null)
                    {
                        if (!destroying)
                        {
                            Write(new CloseMessage());
                        }

                        Stream.Flush();
                        Reader.Close();
                        //_tcpClient.Close();
                        _tcpClient = null;
                    }
                }
            }

            public void Acquire()
            {
                lock (this)
                {
                    if (_cancellationToken != null)
                    {
                        _cancellationToken.Cancel();
                    }
                    _semaphore.WaitOne();
                }
            }

            public void Release()
            {
                lock (this)
                {
                    _cancellationToken = new CancellationTokenSource();
                    _semaphore.Release();
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
                byte[] raw = data as byte[];
                if (raw != null)
                {
                    Stream.Write(raw, 0, raw.Length);
                }
                else
                {
                    MemoryStream stream = new MemoryStream();
                    var writer = new BinaryWriter(stream);
                    NetSerializer<T>.Write(data, writer);
                    writer.Flush();
                    int l = (int)stream.Position;
                    stream.Position = 0;

                    Stream.Write(stream.GetBuffer(), 0, l);
                }
                Stream.Flush();
            }

            public T Read<T>()
            {
                return NetSerializer<T>.Read(Reader);
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
                // Locking
                _client.Acquire();
            }
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

        public static void Close(IPAddress address, ushort port)
        {
            IPEndPoint endPoint = new IPEndPoint(address, port);
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