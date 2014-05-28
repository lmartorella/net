using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Lucky.Home.Core.Serialization;

namespace Lucky.Home.Core
{
    /// <summary>
    /// Base class for sinks
    /// </summary>
    public class Sink : IEquatable<Sink>
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

            private readonly BinaryReader _reader;

            public Connection(IPEndPoint endPoint)
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(endPoint);
                _clientStream = _tcpClient.GetStream();
                _reader = new BinaryReader(_clientStream);
            }

            public void Write<T>(T data)
            {
                MemoryStream stream = new MemoryStream();
                var writer = new BinaryWriter(stream);
                NetSerializer<T>.Write(data, writer);
                writer.Flush();
                int l = (int)stream.Position;
                stream.Position = 0;

                _clientStream.Write(stream.GetBuffer(), 0, l);
                _clientStream.Flush();
            }

            public T Read<T>()
            {
                return NetSerializer<T>.Read(_reader);
            }

            /// <summary>
            /// Close the TCP client connection
            /// </summary>
            public void Dispose()
            {
                if (_tcpClient != null)
                {
                    _clientStream.Flush();
                    _reader.Close();
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
