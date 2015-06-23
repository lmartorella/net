using System;
using System.Net;

namespace Lucky.Home.Core.Protocol
{
    class NodeMessageEventArgs : EventArgs
    {
        public Guid Guid { get; private set; }
        public IPAddress Address { get; private set; }
        public bool IsNew { get; private set; }
        public ushort ControlPort { get; private set; }

        public NodeMessageEventArgs(Guid guid, IPAddress address, bool isNew, ushort controlPort)
        {
            IsNew = isNew;
            ControlPort = controlPort;
            Guid = guid;
            Address = address;
        }
    }
}
