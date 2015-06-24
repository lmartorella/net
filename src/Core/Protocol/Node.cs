using System;
using System.Threading.Tasks;

namespace Lucky.Home.Core.Protocol
{
    class TcpNode : ITcpNode
    {
        /// <summary>
        /// The Unique ID of the node, cannot be empty 
        /// </summary>
        public Guid Id { get; private set; }

        private TcpNodeAddress _address;

        private readonly object _lockObject = new object();
        private bool _inFetchSinkData;
        private static readonly TimeSpan RetryTime = TimeSpan.FromSeconds(1);

        internal TcpNode(Guid guid, TcpNodeAddress address)
        {
            if (guid == Guid.Empty)
            {
                throw new ArgumentNullException("guid");
            }

            Id = guid;
            _address = address;
        }

        private ILogger Logger
        {
            get
            {
                return Manager.GetService<ILogger>();
            }
        }

        public void Heartbeat(TcpNodeAddress address)
        {
            // Update address!
            lock (_address)
            {
                _address = address;
            }
        }

        /// <summary>
        /// An already logged-in node relogs in (e.g. after node reset)
        /// </summary>
        public Task Relogin(TcpNodeAddress address)
        {
            // Update address!
            lock (_address)
            {
                _address = address;
            }
            return FetchSinkData();
        }

        public void RefetchChildren(TcpNodeAddress address)
        {
            throw new NotImplementedException();
        }

        private class CloseMessage
        {
            [SerializeAsFixedArray(4)]
            public string Cmd = "CLOS";
        }

        private class GetChildrenMessage
        {
            [SerializeAsFixedArray(4)]
            public string Cmd = "CHIL";
        }

        private class GetChildrenMessageResponse
        {
            [SerializeAsDynArray]
            public Guid[] Guids;
        }

        internal async Task FetchSinkData()
        {
            lock (_lockObject)
            {
                if (_inFetchSinkData)
                {
                    return;
                }
                _inFetchSinkData = true;
            }
            while (!await FetchSinkDataSingle())
            {
                Task.Delay(RetryTime);
            }
            lock (_lockObject)
            {
                _inFetchSinkData = false;
            }
        }

        private async Task<bool> FetchSinkDataSingle()
        {
            // Init a METADATA fetch connection
            TcpNodeAddress address;
            lock (_address)
            {
                address = _address.Clone();
            }

            try
            {
                using (var connection = new TcpConnection(address.Address, address.ControlPort))
                {
                    connection.Write(new GetChildrenMessage());
                    var childNodes = connection.Read<GetChildrenMessageResponse>();

                    for (int index = 0; index < childNodes.Guids.Length; index++)
                    {
                        var childGuid = childNodes.Guids[index];
                        if (index == 0)
                        {
                            // Mine
                            if (childGuid != Id)
                            {
                                // ERROR
                                Logger.Warning("InvalidGuidInEnum", "Ïd", Id, "returned", childGuid);
                            }
                        }
                        else
                        {
                            // Register a subnode
                            yield Manager.GetService<INodeRegistrar>().RegisterNode(childGuid, _address.SubNode(index));
                            Store it (for future warm registration)
                        }
                    }

                    connection.Write(new CloseMessage());
                }
            }
            catch (Exception exc)
            {
                Logger.Exception(exc);
                return false;
            }

            return true;
        }

        internal Task Rename()
        {
            return new Task(() => { });
        }
    }
}
