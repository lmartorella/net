using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Lucky.Home.Serialization;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// Supports client re-creation in the middle of a communication session
    /// </summary>
    internal class TcpClientManager
    {
        private static readonly ILogger Logger = Manager.GetService<LoggerFactory>().Create("TcpConnection");
        private readonly Dictionary<IPEndPoint, Client> _clients = new Dictionary<IPEndPoint, Client>();
        private readonly object _lockObject = new object();

        internal class Client
        {
            private readonly IPEndPoint _endPoint;
            private readonly TcpClientManager _owner;
            private TcpClient _tcpClient;
            private readonly Stream _stream;
            private readonly BinaryReader _reader;
            private readonly BinaryWriter _writer;

            public Client(IPEndPoint endPoint, TcpClientManager owner)
            {
                _endPoint = endPoint;
                _owner = owner;
                _tcpClient = new TcpClient();
                _tcpClient.Connect(endPoint);
                _tcpClient.NoDelay = true;

                _stream = _tcpClient.GetStream();
#if DEBUG
                // Make client to terminate if read stalls for more than 5 seconds (e.g. sink dead)
                _stream.ReadTimeout = 5000;
#endif

                _reader = new BinaryReader(_stream);
                _writer = new BinaryWriter(_stream);
            }

            public void Close()
            {
                if (_tcpClient != null)
                {
                    _writer.Flush();
                    _stream.Flush();
                    _reader.Close();
                    _tcpClient = null;
                }
            }

            public void Write<T>(T data)
            {
                try
                {
                    NetSerializer<T>.Write(data, _writer);
                    _writer.Flush();
                }
                catch (Exception exc)
                {
                    Logger.Exception(new InvalidDataException("Exception writing object of type " + typeof(T).Name, exc));
                    // Destroy the channel
                    _owner.Abort(_endPoint);
                }
            }

            public void WriteBytes(byte[] data)
            {
                _writer.Write(data, 0, data.Length);
                _writer.Flush();
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
                    _owner.Abort(_endPoint);
                    return default(T);
                }
            }

            public byte[] ReadBytes(int byteCount)
            {
                return _reader.ReadBytes(byteCount);
            }
        }

        public Client GetClient(IPEndPoint endPoint)
        {
            lock (_lockObject)
            {
                Client client;
                if (!_clients.TryGetValue(endPoint, out client))
                {
                    // Destroy the channel
                    client = new Client(endPoint, this);
                    _clients[endPoint] = client;
                }
                return client;
            }
        }

        internal void Abort(IPEndPoint endPoint)
        {
            lock (_lockObject)
            {
                Client client;
                if (_clients.TryGetValue(endPoint, out client))
                {
                    // Destroy the channel
                    client.Close();
                    _clients.Remove(endPoint);
                }
            }
        }
    }
}