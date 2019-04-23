using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Lucky.IO;
using Lucky.Net;
using Lucky.Services;

namespace Lucky.Home.Admin
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class AdminListener : ServiceBase
    {
        private TcpListener _listener;
        private readonly AdminServer _adminInterface = new AdminServer();

        public AdminListener()
        {
            var loopbackAddress = Dns.GetHostAddresses("localhost").FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
            if (loopbackAddress == null)
            {
                Logger.Exception(new InvalidOperationException("Cannot find the loopback address of the host for admin connections"));
            }
            else
            {
                _listener = Manager.GetService<TcpService>().CreateListener(loopbackAddress, Constants.DefaultAdminPort, "Admin", HandleConnection);
            }
        }

        private async void HandleConnection(NetworkStream stream)
        {
            var channel = new MessageChannel(stream);
            while (true)
            {
                var msg = await Receive(channel);
                if (msg == null)
                {
                    // EOF
                    break;
                }

                // Decode message
                Task task = (Task)_adminInterface.GetType().GetMethod(msg.Method).Invoke(_adminInterface, msg.Arguments);
                await task;
                object res = null;
                var args = task.GetType().GetGenericArguments();
                if (args.Length > 0 && args[0].Name != "VoidTaskResult")
                {
                    res = ((dynamic)task).Result;
                }

                await Send(channel, new MessageResponse { Value = res });
            }
        }

        private async Task Send(MessageChannel stream, MessageResponse message)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                MessageResponse.DataContractSerializer.WriteObject(ms, message);
                ms.Flush();
                await stream.WriteMessage(ms.ToArray());
            }
        }

        private async Task<MessageRequest> Receive(MessageChannel stream)
        {
            var buffer = await stream.ReadMessage();
            if (buffer == null)
            {
                return null;
            }
            using (MemoryStream ms = new MemoryStream(buffer))
            {
                return (MessageRequest)MessageRequest.DataContractSerializer.ReadObject(ms);
            }
        }
    }
}
