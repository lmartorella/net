using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Lucky.Home.Core
{
    class Server : ServiceBase, IServer, IDisposable
    {
        private Dictionary<Guid, Peer> _peers = new Dictionary<Guid, Peer>();
        private int DefaultPort = 17008;
        private TcpListener _serviceListener;

        public Server()
        {
            // Find the public IP
            HostAddress = Dns.GetHostAddresses(Dns.GetHostName()).FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));
            if (HostAddress == null)
            {
                throw new InvalidOperationException("Cannot find a public IP address of the host");
            }
            ServicePort = DefaultPort;
            _serviceListener = new TcpListener(HostAddress, ServicePort);
            _serviceListener.Start();
            _serviceListener.BeginAcceptSocket(HandleServiceSocketAccepted, null);
            Logger.Log("Opened Server", "host", HostAddress, "Port", ServicePort);
        }

        public void Dispose()
        {
            _serviceListener.Stop();
        }

        #region IServer interface implementation 

        /// <summary>
        /// Get the public host address
        /// </summary>
        public IPAddress HostAddress { get; private set; }

        /// <summary>
        /// Get the public host service port (TCP)
        /// </summary>
        public int ServicePort { get; private set; }

        #endregion

        private void HandleServiceSocketAccepted(IAsyncResult result)
        {
            var socket = _serviceListener.EndAcceptSocket(result);
            // Read service command

        }
    }
}
