using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Home.Serialization;
using Lucky.Home.Sinks;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    class TcpNode : ITcpNode
    {
        /// <summary>
        /// The Unique ID of the node, cannot be empty 
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// If some active connection action was previously failed, and not yet restored by a heartbeat
        /// </summary>
        public bool IsZombie { get; private set; }

        private readonly object _lockObject = new object();
        private bool _inFetchSinkData;
        private bool _inRename;
        private static readonly TimeSpan RetryTime = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Valid sinks
        /// </summary>
        private readonly List<SinkBase> _sinks = new List<SinkBase>();

        internal TcpNode(Guid guid, TcpNodeAddress address)
        {
            Id = guid;
            Address = address;
            Logger = Manager.GetService<ILoggerFactory>().Create("Node:" + guid);
            
            // Start data fetch asynchrously
            Logger.Log("Fetching metadata");
            FetchMetadata();
        }

        public TcpNodeAddress Address { get; private set; }

        private ILogger Logger { get; set; }

        public void Heartbeat(TcpNodeAddress address)
        {
            // Update address!
            lock (Address)
            {
                Address = address;
            }
            IsZombie = false;
        }

        /// <summary>
        /// An already logged-in node relogs in (e.g. after node reset)
        /// </summary>
        public async Task Relogin(TcpNodeAddress address)
        {
            // Update address!
            lock (Address)
            {
                Address = address;
            }
            IsZombie = false;

            // Start data fetch asynchrously
            await FetchMetadata();
        }

        /// <summary>
        /// Children node collection changed (e.g. new node). Check differences
        /// </summary>
        public void RefetchChildren(TcpNodeAddress address)
        {
            throw new NotImplementedException();
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

        private class NewGuidMessage
        {
            [SerializeAsFixedString(4)]
            public string Cmd = "GUID";

            public readonly Guid Guid;

            public NewGuidMessage(Guid guid)
            {
                Guid = guid;
            }
        }

        private class GetSinksMessageResponse
        {
            [SerializeAsDynArray]
            [SerializeAsFixedString(4)]
            public string[] Sinks;
        }

        private class WriteDataMessage
        {
            [SerializeAsFixedString(4)]
            public string Cmd = "WRIT";

            public short SinkIndex;
        }

        private class ReadDataMessage
        {
            [SerializeAsFixedString(4)]
            public string Cmd = "READ";

            public short SinkIndex;
        }

        private async Task FetchMetadata()
        {
            lock (_lockObject)
            {
                if (_inFetchSinkData)
                {
                    return;
                }
                _inFetchSinkData = true;
            }
            while (!await TryFetchMetadata())
            {
                await Task.Delay(RetryTime);
            }
            lock (_lockObject)
            {
                _inFetchSinkData = false;
            }
        }

        private async Task<bool> OpenNodeSession(Func<TcpConnection, TcpNodeAddress, bool> handler)
        {
            if (IsZombie)
            {
                // Avoid thrashing the network
                return false;
            }

            // Init a METADATA fetch connection
            TcpNodeAddress address;
            lock (Address)
            {
                address = Address.Clone();
            }

            try
            {
                using (var connection = new TcpConnection(address.Address, address.ControlPort))
                {
                    // Connecion can be recycled
                    connection.Write(new SelectNodeMessage(address.Index));
                    return handler(connection, address);
                }
            }
            catch (Exception exc)
            {
                // Forbicly close the channel
                TcpConnection.Close(address.Address, address.ControlPort);
                // Log exc
                Logger.Exception(exc);
                // Mark the node as a zombie
                IsZombie = true;
                return false;
            }
        }

        private async Task<bool> TryFetchMetadata()
        {
            // Init a METADATA fetch connection
            string[] sinks = null;
            if (!await OpenNodeSession((connection, address) =>
            {
                if (!address.IsSubNode)
                {
                    // Ask for subnodes
                    connection.Write(new GetChildrenMessage());
                    var childNodes = connection.Read<GetChildrenMessageResponse>();
                    if (childNodes == null)
                    {
                        return false;
                    }

                    for (int index = 0; index < childNodes.Guids.Length; index++)
                    {
                        var childGuid = childNodes.Guids[index];
                        if (index == 0)
                        {
                            // Mine
                            if (childGuid != Id)
                            {
                                // ERROR
                                Logger.Warning("InvalidGuidInEnum", "Id", Id, "returned", childGuid);
                            }
                        }
                        else
                        {
                            // Register a subnode
                            Manager.GetService<INodeManager>().RegisterNode(childGuid, Address.SubNode(index));
                        }
                    }
                }

                // Then ask for sinks
                connection.Write(new GetSinksMessage());
                var response = connection.Read<GetSinksMessageResponse>();
                if (response == null)
                {
                    return false;
                }
                sinks = response.Sinks;
                return true;
            }))
            {
                return false;
            }

            // Now register sinks
            RegisterSinks(sinks);
            return true;
        }

        private void RegisterSinks(string[] sinks)
        {
            Logger.Log("Registering sinks");
            var sinkManager = Manager.GetService<SinkManager>();

            // Identity of sink is due to its position in the sink array.
            // Null sink are valid, it means no sink at that position. Useful for dynamic add/remove of sinks.
            lock (_sinks)
            {
                ClearSinks();
                _sinks.AddRange(sinks.Select((s, i) => sinkManager.CreateSink(s, this, i)));
            }
        }

        private void ClearSinks()
        {
            var sinkManager = Manager.GetService<SinkManager>();

            lock (_sinks)
            {
                foreach (var sink in _sinks)
                {
                    sinkManager.DestroySink(sink);
                }
                _sinks.Clear();
            }
        }

        /// <summary>
        /// Change the ID of the node
        /// </summary>
        public async Task<bool> Rename(Guid newId)
        {
            if (newId == Guid.Empty)
            {
                throw new ArgumentNullException("newId");
            }
            lock (_lockObject)
            {
                if (_inRename)
                {
                    return false;
                }
                if (newId == Id)
                {
                    return true;
                }
                _inRename = true;
            }

            bool success = false;
            Guid oldId = Guid.Empty;
            try
            {
                // Notify the node registrar too
                Manager.GetService<INodeManager>().BeginRenameNode(this, newId);

                await OpenNodeSession((connection, address) =>
                {
                    connection.Write(new NewGuidMessage(newId));
                    return true;
                });

                success = true;
                oldId = Id;
                Id = newId;
            }
            catch (Exception exc)
            {
                success = false;
                Logger.Exception(exc);
            }
            finally
            {
                lock (_lockObject)
                {
                    _inRename = false;
                }
                Manager.GetService<INodeManager>().EndRenameNode(this, oldId, newId, success);
            }
            return success;
        }

        public Task WriteToSink(int sinkId, Action<IConnectionWriter> writeHandler)
        {
            return OpenNodeSession((connection, address) =>
            {
                // Open stream
                connection.Write(new WriteDataMessage { SinkIndex = (short) sinkId });
                // Now the channel is owned by the sink driver
                // Returns when done, and the protocol should leave the channel clean
                writeHandler(connection);
                return true;
            });
        }

        public Task ReadFromSink(int sinkId, Action<IConnectionReader> readHandler)
        {
            return OpenNodeSession((connection, address) =>
            {
                // Open stream
                connection.Write(new ReadDataMessage {SinkIndex = (short) sinkId});
                readHandler(connection);
                return true;
            });
        }

        public T Sink<T>() where T : ISink
        {
            lock (_sinks)
            {
                return _sinks.OfType<T>().FirstOrDefault();
            }
        }
    }
}
