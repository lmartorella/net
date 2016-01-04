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

    internal class TcpConnection : IConnectionReader, IConnectionWriter
    {
        private static readonly Dictionary<IPEndPoint, Client> s_instances = new Dictionary<IPEndPoint, Client>();
        private static readonly TimeSpan GRACE_TIME = TimeSpan.FromSeconds(10);
        private static readonly ILogger Logger = Manager.GetService<LoggerFactory>().Create("NetSerializer");
        private readonly IPEndPoint _endPoint;

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
            private readonly object _mutex = new object();
            private CancellationTokenSource _cancellationToken;
            private bool _acquired;

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
                // Dispose doesn't need to wait the mutex
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

            public void Acquire()
            {
                Monitor.Enter(_mutex);
                if (_acquired)
                {
                    throw new InvalidOperationException("Already acquired");
                }
                _acquired = true;
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
                    lock (s_instances)
                    {
                        Dispose(false);
                        s_instances.Remove(_endPoint);
                    }
                }, _cancellationToken.Token);

                _acquired = false;
                Monitor.Exit(_mutex);
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
            _endPoint = new IPEndPoint(address, port);
        }

        private Client GetClient()
        {
            lock (s_instances)
            {
                Client client;
                if (!s_instances.TryGetValue(_endPoint, out client))
                {
                    client = new Client(_endPoint);
                    s_instances[_endPoint] = client;
                }
                return client;
            }
        }

        public void Write<T>(T data)
        {
            // Locking
            var client = GetClient();
            client.Acquire();
            try
            {
                client.Write(data);
            }
            finally
            {
                client.Release();
            }
        }

        public T Read<T>()
        {
            // Locking
            var client = GetClient();
            client.Acquire();
            try
            {
                return client.Read<T>();
            }
            finally
            {
                client.Release();
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