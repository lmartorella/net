using System;
using System.Net;

namespace Lucky.Home.Core.Protocol
{
    class NodeMessageEventArgs : EventArgs
    {
        public Guid Guid { get; private set; }
        public IPAddress Address { get; private set; }
        public bool IsNew { get; private set; }

        public NodeMessageEventArgs(Guid guid, IPAddress address, bool isNew)
        {
            IsNew = isNew;
            Guid = guid;
            Address = address;
        }
    }
}
