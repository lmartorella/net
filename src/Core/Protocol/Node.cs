using System;
using System.Net;
using System.Threading.Tasks;

namespace Lucky.Home.Core.Protocol
{
    class Node : INode
    {
        private ushort _controlPort;

        /// <summary>
        /// The Unique ID of the node, cannot be empty 
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// The remote end-point address
        /// </summary>
        private IPAddress _address;

        internal Node(Guid guid, IPAddress address, ushort controlPort)
        {
            if (guid == Guid.Empty)
            {
                throw new ArgumentNullException("guid");
            }

            Id = guid;
            _address = address;
            _controlPort = controlPort;
        }

        public void Heartbeat(IPAddress address, ushort controlPort)
        {
            // Update address!
            lock (_address)
            {
                _address = address;
                _controlPort = controlPort;
            }
        }

        /// <summary>
        /// An already logged-in node relogs in (e.g. after node reset)
        /// </summary>
        public Task Relogin(IPAddress address)
        {
            // Update address!
            lock (_address)
            {
                _address = address;
            }
            return FetchSinkData();
        }

        private class CloseMessage
        {
            [SerializeAsFixedArray(4)]
            public string Cmd = "CLOS";
        }

        private class GetChildrenMessage
        {
            [SerializeAsFixedArray(4)]
            public string Cmd = "CHIL";
        }

        private class GetChildrenMessageResponse
        {
            [SerializeAsDynArray]
            public Guid[] Guids;
        }

        internal Task FetchSinkData()
        {
            // Init a METADATA fetch connection
            IPAddress address;
            ushort controlPort;
            lock (_address)
            {
                address = _address;
                controlPort = _controlPort;
            }

            using (var connection = new TcpConnection(address, controlPort))
            {
                connection.Write(new GetChildrenMessage());
                var children = connection.Read<GetChildrenMessageResponse>();

                connection.Write(new CloseMessage());
            }

            //// Send a HERE packet
            //using (MemoryStream stream = new MemoryStream())
            //{
            //    using (BinaryWriter writer = new BinaryWriter(stream))
            //    {
            //        NetSerializer<HeloAckMessage>.Write(msg, writer);
            //        writer.Flush();

            //        var sender = new UdpClient(AddressFamily.InterNetwork);
            //        IPEndPoint endPoint = new IPEndPoint(address, ackPort);
            //        sender.Send(stream.GetBuffer(), (int)stream.Length, endPoint);
            //    }
            //}

            ///// <summary>
            ///// Peer started, says device capatibilities
            ///// </summary>
            //private void ReadSinkData(Peer peer, ServiceRegisterCommand cmd, ref ServiceResponse responseData)
            //{
            //    foreach (var sinkInfo in cmd.Sinks)
            //    {
            //        Sink sink = Manager.GetService<SinkManager>().CreateSink(sinkInfo.SinkType);
            //        if (sink == null)
            //        {
            //            // Device id unknown!
            //            responseData.ErrCode = ServerErrorCode.UnknownSinkType;
            //            continue;
            //        }

            //        sink.Initialize(peer, sinkInfo.DeviceCaps, sinkInfo.Port);
            //        peer.Sinks.Add(sink);
            //    }

            //    if (peer.ID == Guid.Empty)
            //    {
            //        responseData = new ServiceResponseWithGuid();
            //        // Assign new GUID
            //        responseData.ErrCode = ServerErrorCode.AssignGuid;
            //        var guid = ((ServiceResponseWithGuid)responseData).NewGuid = CreateNewGuid();

            //        Logger.Log("AssignedNewID", "id", guid, "address", peer.Address);
            //    }
            //}
        }

        internal Task Rename()
        {
            return new Task(() => { });
        }
    }
}
