using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Lucky.Home.Core
{
    class Peer : IEquatable<Peer>
    {
        /// <summary>
        /// The Unique ID of the peer, or empty if not initialized
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// The remote end-point address
        /// </summary>
        public IPAddress Address { get; private set; }

        internal Peer(Guid guid, IPAddress address)
        {
            ID = guid;
            Address = address;

            Sinks = new List<Sink>();
        }

        /// <summary>
        /// Get an editable collection of sinks
        /// </summary>
        public ICollection<Sink> Sinks { get; private set; }

        public bool Equals(Peer peer)
        {
            return ID == peer.ID && Address.Equals(peer.Address);
        }
    }
}
