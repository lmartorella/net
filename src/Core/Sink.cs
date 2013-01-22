using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Lucky.Home.Core
{
    /// <summary>
    /// Base class for sinks
    /// </summary>
    class Sink
    {
        private TcpClient _tcpClient;
        private IPAddress _host;
        private Stream _clientStream;

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

        /// <summary>
        /// Open the TCP client connection
        /// </summary>
        protected void Open()
        {
            if (!IsOpen)
            {
                _tcpClient = new TcpClient();
                _tcpClient.Connect(new IPEndPoint(_host, Port));
                _clientStream = _tcpClient.GetStream();
            }
        }

        /// <summary>
        /// Close the TCP client connection
        /// </summary>
        protected void Close()
        {
            if (IsOpen)
            {
                _clientStream.Flush();
                _tcpClient.Close();
                _tcpClient = null;
            }
        }

        protected bool IsOpen
        {
            get
            {
                return _tcpClient != null;
            }
        }

        protected void Send(byte[] buffer, int offset = 0, int count = -1)
        {
            if (count == -1)
            {
                count = buffer.Length - offset;
            }
            _clientStream.Write(buffer, offset, count);
        }
    }
}
