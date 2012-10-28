using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucky.Home.Core
{
    class PeerDiscoveredEventArgs : EventArgs
    {
        public Peer Peer { get; private set; }

        public PeerDiscoveredEventArgs(Peer peer)
        {
            Peer = peer;
        }
    }
}
