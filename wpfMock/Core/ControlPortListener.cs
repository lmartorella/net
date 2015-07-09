using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Lucky.HomeMock.Sinks;

namespace Lucky.HomeMock.Core
{
    class ControlPortListener : IDisposable
    {
        private readonly TcpListener _serviceListener;
        private readonly SinkBase[] _sinks;

        public ControlPortListener(IEnumerable<SinkBase> sinks)
        {
            _sinks = sinks.ToArray();
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

        public ushort Port { get; private set; }

        public void Dispose()
        {
            _serviceListener.Stop();
        }

        public event EventHandler<ItemEventArgs<string>> LogLine;

        private void Log(string str)
        {
            if (LogLine != null)
            {
                LogLine(this, new ItemEventArgs<string>(str));
            }
        }

        class ControlSession : IDisposable
        {
            private readonly Action<string> _logger;
            private readonly ControlPortListener _listener;
            private readonly BinaryWriter _writer;
            private readonly BinaryReader _reader;
            private int _nodeIdx = 0;

            public ControlSession(Stream stream, Action<string> logger, ControlPortListener listener)
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
                _logger("Msg: " + command);
                switch (command)
                {
                    case "CLOS":
                        return false;
                    case "CHIL":
                        Write(1);
                        Write(Data.DeviceId);
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
                        lock (Data.LockObject)
                        {
                            Data.DeviceId = ReadGuid();
                        }
                        _logger("New guid: " + Data.DeviceId);
                        break;
                    case "WRIT":
                        var sinkIdx = ReadUint16();
                        var size = ReadUint16();
                        byte[] data = _reader.ReadBytes(size);
                        _listener._sinks[sinkIdx].Read(data);
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
                    _logger("EXC: " + exc.Message + exc.StackTrace);
                }
            }
        }

        private void HandleServiceSocketAccepted(TcpClient tcpClient)
        {
            using (Stream stream = tcpClient.GetStream())
            {
                Log("Incoming connection from: " + tcpClient.Client.RemoteEndPoint);

                // Now sends message
                using (var session = new ControlSession(stream, Log, this))
                {
                    session.Run();
                }
            }
        }
    }
}
