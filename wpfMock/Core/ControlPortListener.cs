using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Lucky.Home.Core;
using Lucky.HomeMock.Sinks;

namespace Lucky.HomeMock.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class ControlPortListener : ServiceBaseWithData<Data>
    {
        private readonly TcpListener _serviceListener;
        private SinkBase[] _sinks;

        public ControlPortListener()
            :base("ControlPortListener")
        {
            Port = (ushort)new Random().Next(17000, 18000);

            IPAddress hostAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));
            if (hostAddress == null)
            {
                throw new InvalidOperationException("Cannot find a public IP address of the host");
            }

            while (_serviceListener == null)
            {
                try
                {
                    _serviceListener = new TcpListener(hostAddress, Port);
                    _serviceListener.Start();
                }
                catch (SocketException)
                {
                    Port++;
                    _serviceListener = null;
                }
            }

            AsyncCallback handler = null;
            handler = ar =>
            {
                var tcpClient = _serviceListener.EndAcceptTcpClient(ar);
                HandleServiceSocketAccepted(tcpClient);
                _serviceListener.BeginAcceptTcpClient(handler, null);
            };
            _serviceListener.BeginAcceptTcpClient(handler, null);
        }

        public void InitSinks(IEnumerable<SinkBase> sinks)
        {
            _sinks = sinks.ToArray();
        }

        public ushort Port { get; private set; }

        public override void Dispose()
        {
            _serviceListener.Stop();
        }

        class ControlSession : IDisposable
        {
            private readonly ILogger _logger;
            private readonly ControlPortListener _listener;
            private readonly BinaryWriter _writer;
            private readonly BinaryReader _reader;
            private int _nodeIdx = 0;

            public ControlSession(Stream stream, ILogger logger, ControlPortListener listener)
            {
                _logger = logger;
                _listener = listener;
                _writer = new BinaryWriter(stream);
                _reader = new BinaryReader(stream);
            }

            public void Dispose()
            {
                _writer.Dispose();
                _reader.Dispose();
            }

            private string ReadCommand()
            {
                byte[] buffer = new byte[4];
                _reader.Read(buffer, 0, 4);
                return Encoding.ASCII.GetString(buffer);
            }

            private bool RunServer()
            {
                string command = ReadCommand();
                _logger.Log("Msg: " + command);
                ushort sinkIdx;
                byte[] data;
                switch (command)
                {
                    case "CLOS":
                        return false;
                    case "CHIL":
                        Write(1);
                        Write(_listener.State.DeviceId);
                        break;
                    case "SELE":
                        _nodeIdx = ReadUint16();
                        break;
                    case "SINK":
                        Write((ushort)_listener._sinks.Length);
                        foreach (var sink in _listener._sinks)
                        {
                            Write(sink.FourCc);
                        }
                        break;
                    case "GUID":
                        _listener.State = new Data { DeviceId = ReadGuid() };
                        _logger.Log("New guid: " + _listener.State.DeviceId);
                        break;
                    case "WRIT":
                        sinkIdx = ReadUint16();
                        var size = ReadUint16();
                        data = _reader.ReadBytes(size);
                        _listener._sinks[sinkIdx].Read(data);
                        break;
                    case "READ":
                        sinkIdx = ReadUint16();
                        data = _listener._sinks[sinkIdx].Write();
                        _writer.Write((ushort)data.Length);
                        _writer.Write(data);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown protocol command: " + command);
                }

                return true;
            }

            private ushort ReadUint16()
            {
                return _reader.ReadUInt16();
            }

            private Guid ReadGuid()
            {
                return new Guid(_reader.ReadBytes(16));
            }

            private void Write(ushort i)
            {
                _writer.Write(BitConverter.GetBytes(i));
            }

            private void Write(string data)
            {
                _writer.Write(Encoding.ASCII.GetBytes(data));
            }

            private void Write(Guid guid)
            {
                _writer.Write(guid.ToByteArray());
            }

            public void Run()
            {
                try
                {
                    while (true)
                    {
                        if (!RunServer())
                        {
                            break;
                        }
                    }
                }
                catch (Exception exc)
                {
                    _logger.Exception(exc);
                }
            }
        }

        private void HandleServiceSocketAccepted(TcpClient tcpClient)
        {
            using (Stream stream = tcpClient.GetStream())
            {
                Logger.Log("Incoming connection", "from", tcpClient.Client.RemoteEndPoint);

                // Now sends message
                using (var session = new ControlSession(stream, Logger, this))
                {
                    session.Run();
                }
            }
        }
    }
}
