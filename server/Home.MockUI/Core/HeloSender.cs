﻿using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Lucky.Services;

namespace Lucky.HomeMock.Core
{
    class HeloSender : Task
    {
        private readonly ushort _rcvPort;
        private readonly bool _localhostMode;
        private readonly Timer _timer;
        private readonly ILogger _logger;
        public bool ChildChanged = false;

        public HeloSender(ushort rcvPort, bool localhostMode)
        {
            _logger = Manager.GetService<ILoggerFactory>().Create("HeloSender", true);
            _rcvPort = rcvPort;
            _localhostMode = localhostMode;
            // Install an auto-repeat timer until closed
            _timer = new Timer(HandleTick, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
        }

        public override void Dispose()
        {
            _timer.Dispose();
        }

        public bool Registered
        {
            get
            {
                return Manager.GetService<ControlPortListener>().State.DeviceId != Guid.Empty;
            }
        }

        private void HandleTick(object state)
        {
            // Send a broacast HELO message to port 17007
            MemoryStream stream = new MemoryStream();
            var msg = "HEL4";
            if (Registered)
            {
                msg = ChildChanged ? "CCHN" : "HTBT";
            }
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write HOMEHELO
                writer.Write(Encoding.ASCII.GetBytes("HOME" + msg));
                writer.Write(Manager.GetService<ControlPortListener>().State.DeviceId.ToByteArray());
                writer.Write(BitConverter.GetBytes(_rcvPort));
                // If child changed, add a mask for changes
                if (ChildChanged)
                {
                    // Write num of bytes (1)
                    writer.Write(BitConverter.GetBytes((short)1));
                    // Child changed: 1
                    writer.Write((byte)1);
                }
            }
            byte[] dgram = stream.ToArray();
            UdpClient client = new UdpClient();
            var ipAddress = _localhostMode ? new IPAddress(0x0100007f) : IPAddress.Broadcast;

            int port =
#if DEBUG
                17008;
#else
                Constants.UdpControlPort;
#endif
            client.Send(dgram, dgram.Length, new IPEndPoint(ipAddress, port));

            _logger.Log(msg + " sent");
        }
    }
}
