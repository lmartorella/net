using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Lucky.Home.Serialization;
using Lucky.Home.Sinks;
using Lucky.Services;

namespace Lucky.Home.Protocol
{
    class TcpNode : ITcpNode
    {
        private readonly bool _guidShouldBeFetched;

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
        private static readonly TimeSpan RetryTime = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Valid sinks
        /// </summary>
        private readonly List<SinkBase> _sinks = new List<SinkBase>();

        internal TcpNode(Guid guid, TcpNodeAddress address, bool guidShouldBeFetched = false)
        {
            Id = guid;
            Address = address;
            Logger = Manager.GetService<ILoggerFactory>().Create("Node:" + guid);
            _guidShouldBeFetched = guidShouldBeFetched;
        }

        public void Init()
        {
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

        private class GetChildrenMessage
        {
            [SerializeAsFixedString(4)]
            public string Cmd = "CHIL";
        }

        private class GetChildrenMessageResponse
        {
            public Guid Guid;
            [SerializeAsDynArray()]
            public byte[] Mask;
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

            Tuple<bool, TcpNodeAddress[]> ret;
            // Repeat until metadata are OK
            while (!(ret = await TryFetchMetadata()).Item1)
            {
                await Task.Delay(RetryTime);
            }

            // Ok, metadata of the master node are OK
            lock (_lockObject)
            {
                _inFetchSinkData = false;
            }

            // Now register all the children
            RegisterChildrenNodes(ret.Item2);
        }

        private void RegisterChildrenNodes(TcpNodeAddress[] addresses)
        {
            // Now register children
            // Register subnodes, asking for identity
            foreach (var address in addresses)
            {
                Manager.GetService<INodeManager>().RegisterUnknownNode(address);
            }
        }

        private class CloseMessage
        {
            public Fourcc Cmd = new Fourcc("CLOS");
        }

        private static async Task<bool> OpenNodeSession(ILogger logger, TcpNodeAddress address, Func<TcpConnection, TcpNodeAddress, bool> handler)
        {
            // Init a METADATA fetch connection
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
                TcpConnection.Close(new IPEndPoint(address.Address, address.ControlPort));
                // Log exc
                logger.Exception(exc);
                return false;
            }
        }

        private async Task<bool> OpenNodeSession(Func<TcpConnection, TcpNodeAddress, bool> handler)
        {
            if (IsZombie)
            {
                // Avoid thrashing the network
                return false;
            }

            TcpNodeAddress address;
            lock (Address)
            {
                address = Address.Clone();
            }

            var ret = await OpenNodeSession(Logger, address, handler);
            if (!ret)
            {
                // Mark the node as a zombie
                IsZombie = true;
            }
            return ret;
        }

        private async Task<Tuple<bool, TcpNodeAddress[]>> TryFetchMetadata()
        {
            // Init a METADATA fetch connection
            string[] sinks = null;
            byte[] childMask = new byte[0];
            TcpNodeAddress address = null;
            Guid newGuidToAssign = Guid.Empty;

            if (!await OpenNodeSession((connection, addr) =>
            {
                address = addr;

                // Ask for subnodes
                connection.Write(new GetChildrenMessage());
                var childNodes = connection.Read<GetChildrenMessageResponse>();
                if (childNodes == null)
                {
                    // Stop here
                    return false;
                }
                if (childNodes.Guid != Id)
                {
                    if (_guidShouldBeFetched)
                    {
                        newGuidToAssign = childNodes.Guid;
                    }
                    else
                    {
                        // ERROR
                        Logger.Warning("InvalidGuidInEnum", "Id", Id, "returned", childNodes.Guid);
                    }
                }
                childMask = childNodes.Mask;

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
                // Error, no metadata
                return Tuple.Create(false, new TcpNodeAddress[0]);
            }

            if (newGuidToAssign != Guid.Empty)
            {
                // Rename node
                RenameInternal(newGuidToAssign);
            }

            // Now register sinks
            RegisterSinks(sinks);

            var children = ToIntEnumerable(childMask).Select(i => address.SubNode(i + 1)).ToArray();
            return Tuple.Create(true, children);
        }

        private static IEnumerable<int> ToIntEnumerable(byte[] mask)
        {
            int index = 0;
            for (int j = 0; j < mask.Length; j++)
            {
                var b = mask[j];
                for (int i = 0; i < 8; i++, index++, b >>= 1)
                {
                    if ((b & 1) != 0)
                    {
                        yield return index;
                    }
                }
            }
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

        private void RenameInternal(Guid newId)
        {
            Guid oldId = Id;
            Id = newId;
            Manager.GetService<INodeManager>().BeginRenameNode(this, newId);
            Manager.GetService<INodeManager>().EndRenameNode(this, oldId, newId, true);

            Logger.Warning("CorrectGuidAssigned", "oldId", oldId, "newId", newId);
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

        public ISink[] Sinks
        {
            get
            {
                lock (_sinks)
                {
                    return _sinks.ToArray();
                }
            }
        }
    }
}
