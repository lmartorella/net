﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Lucky.Home.Core.Protocol
{
    /// <summary>
    /// Home UDP control service, for discovering devices
    /// </summary>
    internal class ControlPortListener : IDisposable
    {
        //private readonly IPAddress _address;
        private readonly UdpClient _client;

        //private readonly object _lock = new object();
        private readonly ILogger _logger;

        public ControlPortListener(IPAddress address)
        {
            //_address = address;
            _logger = new ConsoleLogger("ControlPortListener");
            _client = new UdpClient(new IPEndPoint(address, Constants.UdpControlPort));
            _client.BeginReceive(OnReceiveData, null);
        }

        public void Dispose()
        {
            _client.Close();
        }

        private enum MessageType
        {
            Unknown,
            Hello,
            Heartebeat
        }

        private void OnReceiveData(IAsyncResult result)
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(0, 0);
            byte[] bytes = _client.EndReceive(result, ref remoteEndPoint);
            if (bytes.Length > 0 && !IPAddress.IsLoopback(remoteEndPoint.Address))
            {
                using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes)))
                {
                    HeloMessage msg;
                    var msgType = DecodeHelloMessage(reader, out msg);
                    if (msgType == MessageType.Unknown)
                    {
                        _logger.Warning("WRONGMSG");
                    }
                    else
                    {
                        bool isNew = msgType == MessageType.Hello;
                        if (!isNew && msg.DeviceId == Guid.Empty)
                        {
                            // Heartbeat of a empty node??
                            _logger.Warning("NOGUID", "addr", remoteEndPoint.Address);
                        }
                        else
                        {
                            _logger.Log("NodeMessage", "ID", msg.DeviceId, "isNew", isNew);
                            if (NodeMessage != null)
                            {
                                NodeMessage(this, new NodeMessageEventArgs(msg.DeviceId, remoteEndPoint.Address, isNew, msg.ControlPort));
                            }
                        }
                    }
                }
            }

            // Enqueue again
            _client.BeginReceive(OnReceiveData, null);
        }

        private MessageType DecodeHelloMessage(BinaryReader reader, out HeloMessage msg)
        {
            msg = NetSerializer<HeloMessage>.Read(reader);
            if (msg == null || msg.Preamble != HeloMessage.PreambleValue)
            {
                return MessageType.Unknown;
            }
            switch (msg.MessageCode)
            {
                case HeloMessage.HeartbeatMessageCode:
                    return MessageType.Heartebeat;
                case HeloMessage.HeloMessageCode:
                    return MessageType.Hello;
                default:
                    return MessageType.Unknown;
            }
        }

        /// <summary>
        /// Event raised when a node is started/powered and says HELLO, or regular heartbeat
        /// </summary>
        public event EventHandler<NodeMessageEventArgs> NodeMessage;
    }
}

