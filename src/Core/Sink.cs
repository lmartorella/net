using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Lucky.Home.Core
{
    /// <summary>
    /// Base class for sinks
    /// </summary>
    class Sink : IEquatable<Sink>
    {
        private IPAddress _host;
        protected readonly Logger Logger = new Logger();

        internal void Initialize(Peer peer, ushort deviceCaps, ushort servicePort)
        {
            _host = peer.Address;
            Port = servicePort;
            DeviceCapabilities = deviceCaps;
            OnInitialize();
        }

        protected virtual void OnInitialize()
        { }

        /// <summary>
        /// Get the service port
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Get the device Caps flags
        /// </summary>
        protected ushort DeviceCapabilities { get; private set; }

        private class Connection : IConnection
        {
            private TcpClient _tcpClient;
            private readonly Stream _clientStream;

            public BinaryReader Reader { get; private set; }
            public BinaryWriter Writer { get; private set; }

            public Connection(IPEndPoint endPoint)
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(endPoint);
                _tcpClient.Client.NoDelay = true;
                _tcpClient.Client.ReceiveTimeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;
                _tcpClient.Client.LingerState = new LingerOption(true, 1);
                _clientStream = _tcpClient.GetStream();
                Reader = new BinaryReader(_clientStream);
                Writer = new BinaryWriter(_clientStream);
            }

            /// <summary>
            /// Close the TCP client connection
            /// </summary>
            public void Dispose()
            {
                if (_tcpClient != null)
                {
                    _clientStream.Flush();
                    Reader.Close();
                    Writer.Close();
                    _tcpClient.Close();
                    _tcpClient = null;
                }
            }
        }

        /// <summary>
        /// Open the TCP client connection
        /// </summary>
        protected IConnection Open()
        {
            return new Connection(new IPEndPoint(_host, Port));
        }

        public override string ToString()
        {
            return "Sink " + GetType().Name + " @" + _host;
        }

        public bool Equals(Sink sink)
        {
            return Port == sink.Port && DeviceCapabilities == sink.DeviceCapabilities;
        }
    }
}
