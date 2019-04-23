using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Lucky.Home.Serialization;
using Lucky.Home.Services;

namespace Lucky.Home.Protocol
{
    /// <summary>
    /// Home UDP control service, for discovering devices
    /// </summary>
    internal class UdpControlPortListener : IDisposable
    {
        //private readonly IPAddress _address;
        private readonly UdpClient _client;

        //private readonly object _lock = new object();
        private readonly ILogger _logger;

        public UdpControlPortListener(IPAddress address)
        {
            int port = Constants.UdpControlPort;
#if DEBUG
            port = Constants.UdpControlPort_Debug;
#endif
            //_address = address;
            _logger = Manager.GetService<ILoggerFactory>().Create("UdpControlPortListener");
            var ep = new IPEndPoint(address, port);
            _client = new UdpClient(ep);
            _logger.Log("Listening", "port", port.ToString() + (port == Constants.UdpControlPort ? " (release)" : (port == Constants.UdpControlPort_Debug ? " (debug)" : " (custom)")));

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            OnReceiveData();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        public void Dispose()
        {
            _client.Close();
        }

        private async Task OnReceiveData()
        {
            while (true)
            {
                var result = await _client.ReceiveAsync();
                byte[] bytes = result.Buffer;
                if (bytes.Length > 0)
                {
                    using (var reader = new MemoryStream(bytes))
                    {
                        var message = await DecodeHelloMessage(reader);
                        var tcpNodeAddress = new TcpNodeAddress(result.RemoteEndPoint.Address, message.HeloMessage.ControlPort, 0);
                        if (message.PingMessageType == PingMessageType.Unknown)
                        {
                            _logger.Warning("WRONGMSG");
                        }
                        else
                        {
                            if (message.PingMessageType != PingMessageType.Heartbeat)
                            {
                                _logger.Log("NodeMessage", "ID", message.HeloMessage.NodeId, "messageType", message.PingMessageType);
                            }
                            NodeMessage?.Invoke(this, new NodeMessageEventArgs(message.HeloMessage.NodeId, tcpNodeAddress, message.PingMessageType, message.ChildrenChanged));
                        }
                    }
                }
            }
        }

        private class DecodedHeloMessage
        {
            public PingMessageType PingMessageType;
            public HeloMessage HeloMessage;
            public int[] ChildrenChanged;
        }

        private async Task<DecodedHeloMessage> DecodeHelloMessage(Stream stream)
        {
            var ret = new DecodedHeloMessage();
            ret.PingMessageType = PingMessageType.Unknown;
            var msg = ret.HeloMessage = await NetSerializer<HeloMessage>.Read(stream);
            if (msg != null && msg.Preamble.Code == HeloMessage.PreambleValue)
            {
                switch (msg.MessageCode.Code)
                {
                    case HeloMessage.HeartbeatMessageCode:
                        ret.PingMessageType = PingMessageType.Heartbeat;
                        break;
                    case HeloMessage.HeloMessageCode:
                        ret.PingMessageType = PingMessageType.Hello;
                        break;
                    case HeloMessage.SubNodeChanged:
                        var mask = (await NetSerializer<HeloSubNodeChangedMessage>.Read(stream))?.Mask;
                        if (mask != null)
                        {
                            ret.ChildrenChanged = TcpNode.DecodeRawMask(mask, i => i);
                            ret.PingMessageType = PingMessageType.SubNodeChanged;
                        }
                        break;
                }
            }
            return ret;
        }

        private class HeloSubNodeChangedMessage
        {
            // Number of children as bit array
            [SerializeAsDynArray]
#pragma warning disable 649
            public byte[] Mask;
#pragma warning restore 649
        }

        /// <summary>
        /// Event raised when a node is started/powered and says HELLO, or regular heartbeat
        /// </summary>
        public event EventHandler<NodeMessageEventArgs> NodeMessage;
    }
}

