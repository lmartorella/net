using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Lucky.Home.Core;

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
                    var msgType = DecodeHelloMessage(reader, out msg);
                    var tcpNodeAddress = new TcpNodeAddress(remoteEndPoint.Address, msg.ControlPort, 0);
                    switch (msgType)
                    {
                        case PingMessageType.Unknown:
                            _logger.Warning("WRONGMSG");
                            break;
                        default:
                            RaiseNodeMessageEvent(msgType, msg.DeviceId, tcpNodeAddress);
                            break;
                    }
                }
            }

            // Enqueue again
            _client.BeginReceive(OnReceiveData, null);
        }

        private void RaiseNodeMessageEvent(PingMessageType messageType, Guid id, TcpNodeAddress address)
        {
            if ((messageType != PingMessageType.Hello) && id == Guid.Empty)
            {
                // Heartbeat of a empty node??
                _logger.Warning("NOGUID", "addr", address);
            }
            else
            {
                _logger.Log("NodeMessage", "ID", id, "messageType", messageType);
                if (NodeMessage != null)
                {
                    NodeMessage(this, new NodeMessageEventArgs(id, address, messageType));
                }
            }
        }

        private PingMessageType DecodeHelloMessage(BinaryReader reader, out HeloMessage msg)
        {
            msg = NetSerializer<HeloMessage>.Read(reader);
            if (msg == null || msg.Preamble != HeloMessage.PreambleValue)
            {
                return PingMessageType.Unknown;
            }
            switch (msg.MessageCode)
            {
                case HeloMessage.HeartbeatMessageCode:
                    return PingMessageType.Heartbeat;
                case HeloMessage.HeloMessageCode:
                    return PingMessageType.Hello;
                case HeloMessage.SubNodeChanged:
                    return PingMessageType.SubNodeChanged;
                default:
                    return PingMessageType.Unknown;
            }
        }

        /// <summary>
        /// Event raised when a node is started/powered and says HELLO, or regular heartbeat
        /// </summary>
        public event EventHandler<NodeMessageEventArgs> NodeMessage;
    }
}

