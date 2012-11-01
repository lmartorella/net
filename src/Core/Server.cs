using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

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

            _serviceListener = TryCreateListener();
            _serviceListener.Start();

            AsyncCallback handler = null;
            handler = ar =>
                    {
                        var tcpClient = _serviceListener.EndAcceptTcpClient(ar);
                        HandleServiceSocketAccepted(tcpClient);
                        _serviceListener.BeginAcceptTcpClient(handler, null);
                    };
            _serviceListener.BeginAcceptTcpClient(handler, null);
            Logger.Log("Opened Server", "host", HostAddress, "Port", ServicePort);
        }

        private TcpListener TryCreateListener()
        {
            do
            {
                try
                {
                    return new TcpListener(HostAddress, ServicePort);
                }
                catch (SocketException)
                {
                    Logger.Log("TCPPortBusy", "port", ServicePort, "trying", ServicePort + 1);
                    ServicePort++;
                }
            } while (true);
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

        private void HandleServiceSocketAccepted(TcpClient tcpClient)
        {
            using (Stream stream = tcpClient.GetStream())
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        // Read service command
                        int l = reader.ReadInt16();
                        byte[] b = reader.ReadBytes(l);
                        string msg = ASCIIEncoding.ASCII.GetString(b);

                        // Write dummy message
                        byte[] msg2 = ASCIIEncoding.ASCII.GetBytes(msg + "? PUPPA!");
                        writer.Write((short)msg2.Length);
                        writer.Write(msg2);
                    }
                }
            }
            tcpClient.Close();
        }
    }
}
