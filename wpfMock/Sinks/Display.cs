using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Lucky.HomeMock.Sinks
{
    class Display : IDisposable
    {
        private TcpListener _serviceListener;

        public Display(ushort port)
        {
            Port = port;
            DeviceCaps = 0;
            DeviceID = 1;

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

        public void Dispose()
        {
            _serviceListener.Stop();
        }

        public ushort Port { get; private set; }

        public ushort DeviceCaps { get; private set; }

        public ushort DeviceID { get; private set; }

        private void HandleServiceSocketAccepted(TcpClient tcpClient)
        {
            using (Stream stream = tcpClient.GetStream())
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    int l = reader.ReadInt16();
                    string str = ASCIIEncoding.ASCII.GetString(reader.ReadBytes(l));
                    if (Data != null)
                    {
                        Data(this, new DataEventArgs(str));
                    }
                }
            }
        }

        public event EventHandler<DataEventArgs> Data;
    }

    public class DataEventArgs : EventArgs
    {
        public readonly string Str;
        public DataEventArgs(string str)
        {
            Str = str;
        }
    }
}
