using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lucky.HomeMock.Sinks
{
    abstract class SinkBase : IDisposable
    {
        private TcpListener _serviceListener;

        public SinkBase(ushort port, ushort caps, ushort deviceId)
        {
            Port = port;
            DeviceCaps = caps;
            DeviceID = deviceId;

            IPAddress hostAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));
            if (hostAddress == null)
            {
                throw new InvalidOperationException("Cannot find a public IP address of the host");
            }
            _serviceListener = new TcpListener(hostAddress, port);
            _serviceListener.Start();

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

        public ushort DeviceCaps { get; private set; }

        public ushort DeviceID { get; private set; }

        public void Dispose()
        {
            _serviceListener.Stop();
        }

        private void HandleServiceSocketAccepted(TcpClient tcpClient)
        {
            using (Stream stream = tcpClient.GetStream())
            {
                OnSocketOpened(stream);
            }
        }

        protected abstract void OnSocketOpened(Stream stream);
    }
}
