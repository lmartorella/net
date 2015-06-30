using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Threading;

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

        private string ReadCommand(BinaryReader reader)
        {
            byte[] buffer = new byte[4];
            reader.Read(buffer, 0, 4);
            return Encoding.ASCII.GetString(buffer);
        }

        private void RunServer(BinaryReader reader, BinaryWriter writer)
        {
            string command = ReadCommand(reader);
            switch (command)
            {
                case "CLOS":
                    return;

                default:
                    throw new InvalidOperationException("Unknown protocol command: " + command);
            }

            // Write RGST command header
            writer.Write(Encoding.ASCII.GetBytes("RGST"));
            // Num of peers
            writer.Write((short)_sinks.Length);

            foreach (ControlPortListener sink in _sinks)
            {
                // Peer device ID
                writer.Write(sink.DeviceID);
                // Peer device capatibilities
                writer.Write(sink.DeviceCaps);
                // PORT
                writer.Write(sink.Port);
            }

        }

        private void HandleServiceSocketAccepted(TcpClient tcpClient)
        {
            using (Stream stream = tcpClient.GetStream())
            {
                Log("Incoming connection from: " + tcpClient.Client.RemoteEndPoint);

                // Now sends message
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        try
                        {
                            RunServer(reader, writer);
                        }
                        catch (Exception exc)
                        {
                            Log("EXC: " + exc.Message + exc.StackTrace);
                        }
                    }
                }
            }
        }
    }
}
