using System;

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
