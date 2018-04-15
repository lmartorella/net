﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Lucky.Home.Serialization;
using Lucky.Services;
using Lucky.Net;

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
            private Stream _stream;
            private readonly BinaryReader _reader;
            private readonly BinaryWriter _writer;
            private readonly TcpClient _tcpClient;
            private bool _disposed;

            public Client(IPEndPoint endPoint, TcpClientManager owner)
            {
                _endPoint = endPoint;
                _owner = owner;
                _tcpClient = Manager.GetService<TcpService>().CreateTcpClient(endPoint);
                _stream = _tcpClient.GetStream();

                // Make client to terminate if read stalls for more than 5 seconds (e.g. sink dead)
                _stream.ReadTimeout = 5000;

                _reader = new BinaryReader(_stream);
                _writer = new BinaryWriter(_stream);
            }

            public bool IsDisposed
            {
                get
                {
                    return _disposed || !_tcpClient.Connected;
                }
            }

            private void Flush()
            {
                _writer.Flush();
                _stream.Flush();
            }

            public void Close()
            {
                if (_stream != null)
                {
                    Flush();
                    _reader.Close();
                    _stream = null;
                    _disposed = true;
                }
            }

            public void Write<T>(T data)
            {
                try
                {
                    NetSerializer<T>.Write(data, _writer);
                    Flush();
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
                Flush();
            }

            public T Read<T>()
            {
                try
                {
                    return NetSerializer<T>.Read(_reader);
                }
                catch (IOException)
                {
                    // Destroy the channel
                    _owner.Abort(_endPoint);
                    return default(T);
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
                    try
                    {
                        client = new Client(endPoint, this);
                        _clients[endPoint] = client;
                    }
                    catch (SocketException exc)
                    {
                        if (exc.ErrorCode == 10061)
                        {
                            // Cannot connect
                            Logger.Log("Cannot Connect", "EP", endPoint);
                            return null;
                        }
                        else
                        {
                            throw;
                        }
                    }
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