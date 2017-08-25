using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Lucky.Home.Serialization;
using Lucky.Services;

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
            var portConfig = Manager.GetService<IConfigurationService>().GetConfig("listen");
            if (portConfig != null)
            {
                port = int.Parse(portConfig);
            }

            //_address = address;
            _logger = Manager.GetService<ILoggerFactory>().Create("UdpControlPortListener");
            _client = new UdpClient(new IPEndPoint(address, port));
            _client.BeginReceive(OnReceiveData, null);

            _logger.Log("Listening", "port", port.ToString() + (port == Constants.UdpControlPort ? " (release)" : (port == Constants.UdpControlPort_Debug ? " (debug)" : " (custom)")));
        }

        public void Dispose()
        {
            _client.Close();
        }

        private void OnReceiveData(IAsyncResult result)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(0, 0);
            byte[] bytes = _client.EndReceive(result, ref remoteEndPoint);
            if (bytes.Length > 0)
            {
                using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes)))
                {
                    HeloMessage msg;
                    int[] childrenChanged;
                    var msgType = DecodeHelloMessage(reader, out msg, out childrenChanged);
                    var tcpNodeAddress = new TcpNodeAddress(remoteEndPoint.Address, msg.ControlPort, 0);
                    if (msgType == PingMessageType.Unknown)
                    {
                        _logger.Warning("WRONGMSG");
                    }
                    else
                    {
                        if (msgType != PingMessageType.Heartbeat)
                        {
                            _logger.Log("NodeMessage", "ID", msg.NodeId, "messageType", msgType);
                        }
                        NodeMessage?.Invoke(this, new NodeMessageEventArgs(msg.NodeId, tcpNodeAddress, msgType, childrenChanged));
                    }
                }
            }

            // Enqueue again
            _client.BeginReceive(OnReceiveData, null);
        }


        private PingMessageType DecodeHelloMessage(BinaryReader reader, out HeloMessage msg, out int[] childrenChanged)
        {
            childrenChanged = null;
            msg = NetSerializer<HeloMessage>.Read(reader);
            if (msg == null || msg.Preamble.Code != HeloMessage.PreambleValue)
            {
                return PingMessageType.Unknown;
            }
            switch (msg.MessageCode.Code)
            {
                case HeloMessage.HeartbeatMessageCode:
                    return PingMessageType.Heartbeat;
                case HeloMessage.HeloMessageCode:
                    return PingMessageType.Hello;
                case HeloMessage.SubNodeChanged:
                    var mask = NetSerializer<HeloSubNodeChangedMessage>.Read(reader)?.Mask;
                    childrenChanged = TcpNode.DecodeRawMask(mask, i => i);
                    return PingMessageType.SubNodeChanged;
                default:
                    return PingMessageType.Unknown;
            }
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

