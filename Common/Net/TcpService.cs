using System;
using System.Net;
using System.Net.Sockets;
using Lucky.Home.Core;

namespace Lucky.Home.Net
{
    /// <summary>
    /// Helper class
    /// </summary>
    public class TcpService : ServiceBase
    {
        private TcpListener TryCreateListener(IPAddress address, ref int startPort)
        {
            do
            {
                try
                {
                    return new TcpListener(address, startPort);
                }
                catch (SocketException)
                {
                    startPort++;
                    Logger.Log("TCPPortBusy", "port", address + ":" + startPort, "trying", startPort);
                }
            } while (true);
        }

        public TcpListener CreateListener(IPAddress address, int startPort, string portName, Action<TcpClient> incomingHandler)
        {
            var listener = TryCreateListener(address, ref startPort);
            Logger.Log("Opened", "socket", portName, "address", address.ToString() + ":" + startPort);

            listener.Start();
            AsyncCallback handler = null;
            handler = ar =>
            {
                var tcpClient = listener.EndAcceptTcpClient(ar);
                incomingHandler(tcpClient);
                listener.BeginAcceptTcpClient(handler, null);
            };
            listener.BeginAcceptTcpClient(handler, null);
            return listener;
        }
    }
}
