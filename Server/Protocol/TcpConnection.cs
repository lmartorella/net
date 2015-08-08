using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Lucky.Home.Core;

namespace Lucky.Home.Protocol
{
    internal class TcpConnection : IDisposable
    {
        private static readonly Dictionary<IPEndPoint, Client> s_instances = new Dictionary<IPEndPoint, Client>();
        private Client _client;
        private static readonly TimeSpan GRACE_TIME = TimeSpan.FromSeconds(4);

        private class Client
        {
            private readonly IPEndPoint _endPoint;
            private TcpClient _tcpClient;
            public readonly Stream Stream;
            public readonly BinaryReader Reader;
            private readonly object _lock = new object();
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

            public void Dispose()
            {
                if (_tcpClient != null)
                {
                    Stream.Flush();
                    Reader.Close();
                    //_tcpClient.Close();
                    _tcpClient = null;
                }
            }

            public void Acquire()
            {
                _cancellationToken.Cancel();
                Monitor.Enter(_lock);
            }

            public void Release()
            {
                Monitor.Exit(_lock);
                // Start timeout auto-disposal timer
                _cancellationToken = new CancellationTokenSource();
                Task.Delay(GRACE_TIME, _cancellationToken.Token).ContinueWith(task =>
                {
                    lock (s_instances)
                    {
                        Dispose();
                        s_instances.Remove(_endPoint);
                    }
                }, _cancellationToken.Token).Start();
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
                else
                {
                    // Locking
                    _client.Acquire();
                }
            }
        }

        public void Write<T>(T data) where T : class
        {
            MemoryStream stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            NetSerializer<T>.Write(data, writer);
            writer.Flush();
            int l = (int) stream.Position;
            stream.Position = 0;

            _client.Stream.Write(stream.GetBuffer(), 0, l);
            _client.Stream.Flush();
        }

        public T Read<T>() where T : class
        {
            return NetSerializer<T>.Read(_client.Reader);
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
    }
}