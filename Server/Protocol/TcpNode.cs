using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Home.Serialization;
using Lucky.Home.Sinks;
using Lucky.Services;

#pragma warning disable 414
#pragma warning disable 649
#pragma warning disable 169

// ReSharper disable ClassNeverInstantiated.Local
// ReSharper disable MemberCanBePrivate.Local
// ReSharper disable NotAccessedField.Local

namespace Lucky.Home.Protocol
{
    class TcpNode : ITcpNode
    {
        /// <summary>
        /// The Unique ID of the node, cannot be empty 
        /// </summary>
        public Guid Id { get; private set; }

        public bool ShouldBeRenamed { get; private set; }

        private TcpNodeAddress _address;

        private readonly object _lockObject = new object();
        private bool _inFetchSinkData;
        private static readonly TimeSpan RetryTime = TimeSpan.FromSeconds(1);
        private readonly ILogger _logger;

        /// <summary>
        /// If some active connection action was previously failed, and not yet restored by a heartbeat
        /// </summary>
        private bool _isZombie;

        /// <summary>
        /// Valid sinks
        /// </summary>
        private readonly List<Sink> _sinks = new List<Sink>();

        private TcpNodeAddress _address1;

        internal TcpNode(Guid guid, TcpNodeAddress address, bool shouldBeRenamed = false)
        {
            if (guid == Guid.Empty)
            {
                throw new ArgumentNullException("guid");
            }

            Id = guid;
            ShouldBeRenamed = shouldBeRenamed;
            _address = address;
            _logger = Manager.GetService<ILoggerFactory>().Create("Node:" + guid);
        }

        public TcpNodeAddress Address
        {
            get { return _address; }
        }

        private ILogger Logger
        {
            get
            {
                return _logger;
            }
        }

        public void Heartbeat(TcpNodeAddress address)
        {
            // Update address!
            lock (_address)
            {
                _address = address;
            }
            _isZombie = false;
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
            _isZombie = false;
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

        internal async Task FetchMetadata()
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

        private Task<bool> OpenNodeSession(Action<TcpConnection, TcpNodeAddress> handler)
        {
            if (_isZombie)
            {
                // Avoid thrashing the network
                return Task.FromResult(false);
            }

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
                    // Connecion can be recycled
                    connection.Write(new SelectNodeMessage(address.Index));
                    handler(connection, address);
                }
            }
            catch (Exception exc)
            {
                // Forbicly close the channel
                TcpConnection.Close(address.Address, address.ControlPort);
                // Log exc
                Logger.Exception(exc);
                // Mark the node as a zombie
                _isZombie = true;
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
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
                connection.Write(new GetSinksMessage());
                sinks = connection.Read<GetSinksMessageResponse>().Sinks;
            }))
            {
                return false;
            }

            // Now register sinks
            RegisterSinks(sinks);

            return true;
        }

        private async void RegisterSinks(string[] sinks)
        {
            var sinkManager = Manager.GetService<SinkManager>();

            List<ISink> newSinks = new List<ISink>();

            // Identity of sink is due to its position in the sink array.
            // Null sink are valid, it means no sink at that position. Useful for dynamic add/remove of sinks.
            lock (_sinks)
            {
                // Make _sink size equal to new size
                // Trim excess
                while (_sinks.Count > sinks.Length)
                {
                    _sinks.Last().Dispose();
                    _sinks.RemoveAt(_sinks.Count - 1);
                }

                // Now check for new pos
                while (_sinks.Count < sinks.Length)
                {
                    _sinks.Add(null);
                }

                for (int i = 0; i < sinks.Length; i++)
                {
                    var oldSink = _sinks[i];
                    if (oldSink != null)
                    {
                        if (oldSink.FourCc == sinks[i])
                        {
                            // Ok, same sink
                            continue;
                        }
                        // Dispose the old one
                        oldSink.Dispose();
                    }
                    _sinks[i] = sinkManager.CreateSink(sinks[i], Id, i);
                    newSinks.Add(_sinks[i]);
                }
            }
        }

        internal async Task Rename()
        {
            await Task.Run(() =>
            {
                OpenNodeSession((connection, address) =>
                {
                    connection.Write(new NewGuidMessage(Id));
                });
                ShouldBeRenamed = false;
            });
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
            });
        }

        public Task ReadFromSink(int sinkId, Action<IConnectionReader> readHandler)
        {
            return OpenNodeSession((connection, address) =>
            {
                // Open stream
                connection.Write(new ReadDataMessage {SinkIndex = (short) sinkId});
                readHandler(connection);
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
