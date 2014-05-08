using System;

namespace Lucky.Home.Core
{
    interface IHelloListener
    {
        /// <summary>
        /// Event raised when a new peer is started/powered and requires registration
        /// </summary>
        event EventHandler<PeerDiscoveredEventArgs> PeerDiscovered;
    }
}
