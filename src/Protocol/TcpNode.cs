using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lucky.Home.Core;

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

        internal TcpNode(Guid guid, TcpNodeAddress address)
        {
            if (guid == Guid.Empty)
            {
                throw new ArgumentNullException("guid");
            }

            Id = guid;
            _address = address;
            _logger = new ConsoleLogger("Node:" + guid);
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

            [SerializeAsDynArray]
            public byte[] Data;
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

        private bool MakeConnection(Action<TcpConnection, TcpNodeAddress> handler)
        {
            if (_isZombie)
            {
                // Avoid thrashing the network
                return false;
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
                    handler(connection, address);
                    connection.Write(new CloseMessage());
                }
            }
            catch (Exception exc)
            {
                Logger.Exception(exc);
                // Mark it as a zombie
                _isZombie = true;
                return false;
            }
            return true;
        }

        private async Task<bool> TryFetchSinkData()
        {
            return await Task.Run(() =>
            {
                // Init a METADATA fetch connection
                string[] sinks = null;
                if (!MakeConnection((connection, address) =>
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
                }))
                {
                    return false;
                }

                // Now register sinks
                RegisterSinks(sinks);

                return true;
            });
        }

        private void RegisterSinks(string[] sinks)
        {
            var sinkManager = Manager.GetService<SinkManager>();

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
                }
            }
        }

        internal async Task Rename()
        {
            await Task.Run(() =>
            {
                MakeConnection((connection, address) =>
                {
                    connection.Write(new SelectNodeMessage(address.Index));
                    connection.Write(new NewGuidMessage(Id));
                });
            });
        }

        public bool WriteToSink(byte[] data, int sinkId)
        {
            return MakeConnection((connection, address) =>
            {
                // Select subnode
                connection.Write(new SelectNodeMessage(address.Index));
                // Open stream
                connection.Write(new WriteDataMessage {SinkIndex = (short) sinkId, Data = data});
            });
        }
    }
}
