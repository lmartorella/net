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
            //_address = address;
            _logger = Manager.GetService<ILoggerFactory>().Create("UdpControlPortListener");
            _client = new UdpClient(new IPEndPoint(address, Constants.UdpControlPort));
            _client.BeginReceive(OnReceiveData, null);
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
                        _logger.Log("NodeMessage", "ID", msg.DeviceId, "messageType", msgType);
                        NodeMessage?.Invoke(this, new NodeMessageEventArgs(msg.DeviceId, tcpNodeAddress, msgType, childrenChanged));
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
            public byte[] Mask;
        }

        /// <summary>
        /// Event raised when a node is started/powered and says HELLO, or regular heartbeat
        /// </summary>
        public event EventHandler<NodeMessageEventArgs> NodeMessage;
    }
}

