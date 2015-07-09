using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Lucky.Home.Core;

namespace Lucky.Home.Protocol
{
    internal class TcpConnection : IDisposable
    {
        private TcpClient _tcpClient;
        private readonly Stream _clientStream;

        private readonly BinaryReader _reader;

        public TcpConnection(IPAddress address, ushort port)
        {
            IPEndPoint endPoint = new IPEndPoint(address, port);
            _tcpClient = new TcpClient();
            _tcpClient.Connect(endPoint);
            _clientStream = _tcpClient.GetStream();
            //_clientStream.ReadTimeout = 5 * 60 * 1000;
            //_clientStream.WriteTimeout = 5 * 60 * 1000;
            _reader = new BinaryReader(_clientStream);
        }

        public void Write<T>(T data) where T : class
        {
            MemoryStream stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            NetSerializer<T>.Write(data, writer);
            writer.Flush();
            int l = (int) stream.Position;
            stream.Position = 0;

            _clientStream.Write(stream.GetBuffer(), 0, l);
            _clientStream.Flush();
        }

        public T Read<T>() where T : class
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
}