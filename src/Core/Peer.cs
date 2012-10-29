using System;
using System.Net;

namespace Lucky.Home.Core
{
    class Peer
    {
        /// <summary>
        /// The Unique ID of the peer, or empty if not initialized
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// The HTTP service port of the peer
        /// </summary>
        public int ServicePort { get; private set; }

        internal Peer(Guid guid, IPAddress address, int servicePort)
        {
            ID = guid;
            ServicePort = servicePort;
        }
    }
}
