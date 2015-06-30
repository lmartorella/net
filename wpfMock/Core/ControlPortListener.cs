using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Lucky.HomeMock.Core
{
    class ControlPortListener : IDisposable
    {
        private readonly TcpListener _serviceListener;

        public ControlPortListener()
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
            private readonly BinaryWriter _writer;
            private readonly BinaryReader _reader;
            private int _nodeIdx = 0;

            public ControlSession(Stream stream, Action<string> logger)
            {
                _logger = logger;
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
                        Write(0);
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

            private void Write(ushort i)
            {
                _writer.Write(BitConverter.GetBytes(i));
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
                using (var session = new ControlSession(stream, Log))
                {
                    session.Run();
                }
            }
        }
    }
}
