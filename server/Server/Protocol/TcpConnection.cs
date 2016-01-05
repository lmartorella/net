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
    internal class TcpConnection : IDisposable, IConnectionReader, IConnectionWriter
    {
        private static readonly Dictionary<IPEndPoint, Client> s_activeClients = new Dictionary<IPEndPoint, Client>();
        private static readonly Dictionary<IPEndPoint, ClientMutex> s_mutexes = new Dictionary<IPEndPoint, ClientMutex>();
        private static readonly object s_lockObject = new object();

        private static readonly TimeSpan GRACE_TIME = TimeSpan.FromSeconds(10);
        private static readonly ILogger Logger = Manager.GetService<LoggerFactory>().Create("TcpConnection");

        private ClientMutex _mutex;
        private readonly IPEndPoint _endPoint;

        private class CloseMessage
        {
            public Fourcc Cmd = new Fourcc("CLOS");
        }

        private class ClientMutex
        {
            private readonly IPEndPoint _endPoint;
            private readonly Mutex _mutex = new Mutex();
            private CancellationTokenSource _cancellationToken;

            public ClientMutex(IPEndPoint endPoint)
            {
                _endPoint = endPoint;
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
                    Close(_endPoint, true);
                }, _cancellationToken.Token);
                _mutex.ReleaseMutex();
            }
        }

        private class Client
        {
            private readonly IPEndPoint _endPoint;
            private TcpClient _tcpClient;
            private readonly Stream _stream;
            private readonly BinaryReader _reader;

            public Client(IPEndPoint endPoint)
            {
                _endPoint = endPoint;
                _tcpClient = new TcpClient();
                _tcpClient.Connect(endPoint);
                _tcpClient.NoDelay = true;
                _stream = _tcpClient.GetStream();
                _reader = new BinaryReader(_stream);
                // Make client to terminate if read stalls for more than 5 seconds (e.g. sink dead)
                _stream.ReadTimeout = 5000;
            }

            public void Close(bool sendCloseMessage)
            {
                if (_tcpClient != null)
                {
                    if (sendCloseMessage)
                    {
                        Write(new CloseMessage());
                    }

                    _stream.Flush();
                    _reader.Close();
                    _tcpClient = null;
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
                    TcpConnection.Close(_endPoint, false);
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
                    TcpConnection.Close(_endPoint, false);
                    return default(T);
                }
            }
        }

        public TcpConnection(IPAddress address, ushort port)
        {
            _endPoint = new IPEndPoint(address, port);
            lock (s_lockObject)
            {
                if (!s_mutexes.TryGetValue(_endPoint, out _mutex))
                {
                    _mutex = new ClientMutex(_endPoint);
                    s_mutexes[_endPoint] = _mutex;
                }
            }

            // Locking
            _mutex.Acquire();
        }

        /// <summary>
        /// Close the TCP client connection
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
            GetClient().Write(data);
        }

        public T Read<T>()
        {
            return GetClient().Read<T>();
        }

        private Client GetClient()
        {
            Client client;
            if (!s_activeClients.TryGetValue(_endPoint, out client))
            {
                // Destroy the channel
                client = new Client(_endPoint);
                s_activeClients[_endPoint] = client;
            }
            return client;
        }

        public static void Close(IPEndPoint endPoint, bool sendCloseMessage = false)
        {
            lock (s_lockObject)
            {
                Client client;
                if (s_activeClients.TryGetValue(endPoint, out client))
                {
                    // Destroy the channel
                    client.Close(sendCloseMessage);
                    s_activeClients.Remove(endPoint);
                }
                s_mutexes.Remove(endPoint);
            }
        }
    }
}