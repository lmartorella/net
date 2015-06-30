using System;
using System.Threading.Tasks;
using TaskExtensions = NuGet.TaskExtensions;

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
        public async Task Relogin(TcpNodeAddress address)
        {
            // Update address!
            lock (_address)
            {
                _address = address;
            }
            await FetchSinkData();
        }

        /// <summary>
        /// Children node collection changed (e.g. new node). Check differences
        /// </summary>
        public void RefetchChildren(TcpNodeAddress address)
        {
            throw new NotImplementedException();
        }

        private class CloseMessage
        {
            [SerializeAsFixedString(4)]
            public string Cmd = "CLOS";
        }

        private class GetChildrenMessage
        {
            [SerializeAsFixedString(4)]
            public string Cmd = "CHIL";
        }

        private class GetChildrenMessageResponse
        {
            [SerializeAsDynArray]
            public Guid[] Guids;
        }

        private class SelectNodeMessage
        {
            [SerializeAsFixedString(4)]
            public string Cmd = "SELE";

            public short Index;

            public SelectNodeMessage(int index)
            {
                Index = (short)index;
            }
        }

        private class GetSinksMessage
        {
            [SerializeAsFixedString(4)] 
            public string Cmd = "SINK";
        }

        private class GetSinksMessageResponse
        {
            [SerializeAsDynArray]
            [SerializeAsFixedString(4)]
            public string[] Sinks;
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
            while (!await TryFetchSinkData())
            {
                await Task.Delay(RetryTime);
            }
            lock (_lockObject)
            {
                _inFetchSinkData = false;
            }
        }

        private async Task<bool> TryFetchSinkData()
        {
            return await Task.Run(() =>
            {
                // Init a METADATA fetch connection
                TcpNodeAddress address;
                lock (_address)
                {
                    address = _address.Clone();
                }

                string[] sinks;

                try
                {
                    using (var connection = new TcpConnection(address.Address, address.ControlPort))
                    {
                        if (!address.IsSubNode)
                        {
                            // Ask for subnodes
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
                                    Manager.GetService<INodeRegistrar>().RegisterNode(childGuid, _address.SubNode(index));
                                }
                            }
                        }

                        // Then ask for sinks
                        connection.Write(new SelectNodeMessage(address.Index));
                        connection.Write(new GetSinksMessage());
                        sinks = connection.Read<GetSinksMessageResponse>().Sinks;

                        connection.Write(new CloseMessage());
                    }
                }
                catch (Exception exc)
                {
                    Logger.Exception(exc);
                    return false;
                }

                // Now register sinks
                RegisterSinks(sinks);

                return true;
            });
        }

        private void RegisterSinks(string[] sinks)
        {
            throw new NotImplementedException();
        }

        internal Task Rename()
        {
            return new Task(() => { });
        }
    }
}
