using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Lucky.HomeMock.Sinks;
using Lucky.Services;

namespace Lucky.HomeMock.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class ControlPortListener : ServiceBaseWithData<Data>
    {
        private readonly TcpListener _serviceListener;
        private SinkMockBase[] _sinks;

        public ControlPortListener()
        {
            Port = (ushort)new Random().Next(17000, 18000);

            IPAddress hostAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));
            if (hostAddress == null)
            {
                hostAddress = new IPAddress(0x0100007f);
                LocalhostMode = true;
                Logger.Warning("Cannot find a public IP address of the host. Using loopback.");
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
        }

        public void StartServer()
        {
            System.Threading.Tasks.Task.Run(async () =>
            {
                while (true)
                {
                    var tcpClient = await _serviceListener.AcceptTcpClientAsync();
                    HandleServiceSocketAccepted(tcpClient);
                }
            });
        }

        public void InitSinks(IEnumerable<SinkMockBase> sinks)
        {
            _sinks = sinks.ToArray();
        }

        public ushort Port { get; private set; }
        public bool LocalhostMode { get; private set; }

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

            public ControlSession(NetworkStream stream, ILogger logger, ControlPortListener listener)
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
                if (_reader.Read(buffer, 0, 4) < 4)
                {
                    return null;
                }
                return Encoding.ASCII.GetString(buffer);
            }

            private bool RunServer()
            {
                string command = ReadCommand();
                if (command == null)
                {
                    return false;
                }

                _logger.Log("Msg: " + command);
                ushort sinkIdx;
                switch (command)
                {
                    case "CLOS":
                        // Ack
                        _writer.Write(new byte[] { 0x1e });
                        break;
                    case "CHIL":
                        Write(_listener.State.DeviceId);
                        Write(0);
                        break;
                    case "SELE":
                        ReadUint16();
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
                        _listener._sinks[sinkIdx].Read(_reader);
                        break;
                    case "READ":
                        sinkIdx = ReadUint16();
                        _listener._sinks[sinkIdx].Write(_writer);
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
            tcpClient.NoDelay = true;
            using (var stream = tcpClient.GetStream())
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
