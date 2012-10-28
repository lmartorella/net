using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucky.Home.Core
{
    interface IHelloListener : IService
    {
        /// <summary>
        /// Event raised when a new peer is started/powered and requires registration
        /// </summary>
        event EventHandler<PeerDiscoveredEventArgs> PeerDiscovered;
    }
}
