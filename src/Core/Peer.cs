using System;
using System.Net;

namespace Lucky.Home.Core
{
    class Peer
    {
        public Guid ID { get; private set; }

        internal Peer(Guid guid, IPAddress address, int port)
        {
            ID = guid;
        }
    }
}
