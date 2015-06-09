using System;
using System.Net;

namespace Lucky.Home.Core.Protocol
{
    class Node
    {
        /// <summary>
        /// The Unique ID of the node, cannot be empty 
        /// </summary>
        public Guid ID { get; private set; }

        /// <summary>
        /// The remote end-point address
        /// </summary>
        public IPAddress Address { get; private set; }

        internal Node(Guid guid, IPAddress address)
        {
            if (guid == Guid.Empty)
            {
                throw new ArgumentNullException("guid");
            }

            ID = guid;
            Address = address;

            //Sinks = new List<Sink>();
        }

        ///// <summary>
        ///// Get an editable collection of sinks
        ///// </summary>
        //public ICollection<Sink> Sinks { get; private set; }

        //public bool Equals(Node peer)
        //{
        //    return ID == peer.ID && Address.Equals(peer.Address);
        //}
    }
}
