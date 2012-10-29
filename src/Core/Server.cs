using System;
using System.Collections.Generic;
using System.Net;

namespace Lucky.Home.Core
{
    class Server : IService, IServer
    {
        private Dictionary<Guid, Peer> _peers = new Dictionary<Guid, Peer>();

        public Server()
        {
            allocatefreeport();
        }

        #region IServer interface implementation 

        /// <summary>
        /// Get the public host address
        /// </summary>
        public IPAddress HostAddress { get; private set; }

        /// <summary>
        /// Get the public host service port (TCP)
        /// </summary>
        public int ServicePort { get; private set; }

        #endregion
    }
}
