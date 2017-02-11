using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Lucky.Services;
using System.IO;

namespace Lucky.Net
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

        public TcpListener CreateListener(IPAddress address, int startPort, string portName, Action<NetworkStream> incomingHandler)
        {
            var listener = TryCreateListener(address, ref startPort);
            Logger.Log("Opened", "socket", portName, "address", address + ":" + startPort);

            listener.Start();
            AsyncCallback handler = null;
            handler = ar =>
            {
                var tcpClient = listener.EndAcceptTcpClient(ar);
                Task.Run(() =>
                {
                    try
                    {
                        incomingHandler(tcpClient.GetStream());
                    }
                    catch (Exception exc)
                    {
                        Logger.Exception(exc);
                    }
                });
                listener.BeginAcceptTcpClient(handler, null);
            };
            listener.BeginAcceptTcpClient(handler, null);
            return listener;
        }

        public Stream CreateTcpClientStream(IPEndPoint endPoint)
        {
            var tcpClient = new TcpClient();
            tcpClient.Connect(endPoint);
            //_tcpClient.NoDelay = true;  // Setting this to true will send single chars in Serializer loops...
            tcpClient.SendTimeout = 1;
            tcpClient.ReceiveTimeout = 1;

            var stream = tcpClient.GetStream();
#if DEBUG
            // Make client to terminate if read stalls for more than 5 seconds (e.g. sink dead)
            stream.ReadTimeout = 5000;
#endif
            return stream;
        }
    }
}
